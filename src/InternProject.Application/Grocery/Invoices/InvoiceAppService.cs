using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using InternProject.Authorization;
using InternProject.Grocery;
using InternProject.Grocery.Invoices.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace InternProject.Grocery.Invoices;

[AbpAuthorize(PermissionNames.Pages_Invoices)]
public class InvoiceAppService : InternProjectAppServiceBase, IInvoiceAppService
{
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<InventoryLog, Guid> _inventoryLogRepository;
    private readonly IRepository<StockBatch, Guid> _stockBatchRepository;

    public InvoiceAppService(
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<InventoryLog, Guid> _inventoryLogRepository,
        IRepository<StockBatch, Guid> stockBatchRepository)
    {
        _invoiceRepository = invoiceRepository;
        _productRepository = productRepository;
        this._inventoryLogRepository = _inventoryLogRepository;
        _stockBatchRepository = stockBatchRepository;
    }

    public async Task<InvoiceDto> GetAsync(EntityDto<Guid> input)
    {
        var invoice = await _invoiceRepository.GetAll()
            .Include(x => x.Customer)
            .Include(x => x.CashierUser)
            .Include(x => x.InvoiceItems)
            .FirstOrDefaultAsync(x => x.Id == input.Id);

        if (invoice == null)
        {
            throw new Abp.Domain.Entities.EntityNotFoundException(typeof(Invoice), input.Id);
        }

        return ObjectMapper.Map<InvoiceDto>(invoice);
    }

    public async Task<PagedResultDto<InvoiceDto>> GetListAsync(PagedInvoiceResultRequestDto input)
    {
        IQueryable<Invoice> query = _invoiceRepository.GetAll()
            .Include(x => x.Customer)
            .Include(x => x.CashierUser);

        query = query.WhereIf(
            !input.Keyword.IsNullOrWhiteSpace(),
            x => x.InvoiceNumber.Contains(input.Keyword) ||
                 (x.Customer != null && x.Customer.Name.Contains(input.Keyword)) ||
                 (x.CashierUser != null && x.CashierUser.UserName.Contains(input.Keyword))
        );

        var totalCount = await query.CountAsync();

        if (!input.Sorting.IsNullOrWhiteSpace())
        {
            query = query.OrderBy(input.Sorting);
        }
        else
        {
            query = query.OrderByDescending(x => x.CreationTime);
        }

        var invoices = await query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();

        return new PagedResultDto<InvoiceDto>(
            totalCount,
            ObjectMapper.Map<List<InvoiceDto>>(invoices)
        );
    }

    [AbpAuthorize(PermissionNames.Pages_Invoices_Create)]
    [Abp.Domain.Uow.UnitOfWork(System.Transactions.IsolationLevel.Serializable)]
    public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto input)
    {
        if (input.InvoiceItems == null || !input.InvoiceItems.Any())
        {
            throw new UserFriendlyException("Hóa đơn phải có ít nhất 1 sản phẩm.");
        }

        // 1. Load all products in one query
        var productIds = input.InvoiceItems.Select(x => x.ProductId).Distinct().ToList();
        var products = await _productRepository.GetAll()
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        // Load all active stock batches for these products
        var batches = await _stockBatchRepository.GetAll()
            .Where(b => productIds.Contains(b.ProductId) && b.RemainingQuantity > 0)
            .ToListAsync();

        // 2. Validate stock & build items with snapshot
        var items = new List<InvoiceItem>();
        decimal totalAmount = 0;

        foreach (var line in input.InvoiceItems)
        {
            var p = products.FirstOrDefault(x => x.Id == line.ProductId)
                ?? throw new UserFriendlyException("Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");

            if (line.Quantity <= 0)
            {
                throw new UserFriendlyException($"Số lượng mua phải lớn hơn 0 ({p.Name}).");
            }

            var productBatches = batches.Where(b => b.ProductId == p.Id).ToList();

            var availableUnexpiredStock = productBatches
                .Where(b => b.ExpiryDate == null || b.ExpiryDate > DateTime.Now)
                .Sum(b => b.RemainingQuantity);

            var totalStock = productBatches.Sum(b => b.RemainingQuantity);

            if (line.Quantity > totalStock)
            {
                throw new UserFriendlyException($"Sản phẩm '{p.Name}' trong kho không đủ số lượng bán.");
            }

            if (line.Quantity > availableUnexpiredStock)
            {
                throw new UserFriendlyException($"Sản phẩm '{p.Name}' có hàng tồn kho nhưng đã quá hạn sử dụng, vui lòng báo Admin thực hiện Hủy lô hết hạn.");
            }

            var subtotal = p.SalePrice * line.Quantity;
            items.Add(new InvoiceItem
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Sku = p.Sku,
                Quantity = line.Quantity,
                UnitPrice = p.SalePrice,
                Subtotal = subtotal,
                InvoiceItemBatches = new List<InvoiceItemBatch>()
            });
            totalAmount += subtotal;
        }

        // 3. Validate payment
        if (input.AmountPaid < totalAmount)
        {
            throw new UserFriendlyException("Số tiền khách đưa không đủ.");
        }

        // 4. Create invoice
        var invoice = new Invoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            CustomerId = input.CustomerId,
            CashierUserId = AbpSession.UserId ?? throw new UserFriendlyException("Không tìm thấy thông tin Thu ngân đăng nhập."),
            TotalAmount = totalAmount,
            AmountPaid = input.AmountPaid,
            ChangeAmount = input.AmountPaid - totalAmount,
            PaymentMethod = input.PaymentMethod,
            Status = InvoiceStatus.Completed,
            Note = input.Note,
            InvoiceItems = items
        };

        var invoiceId = await _invoiceRepository.InsertAndGetIdAsync(invoice);

        // 5. Deduct stock + write InventoryLog for each batch
        foreach (var item in items)
        {
            var p = products.First(x => x.Id == item.ProductId);
            var productBatches = batches.Where(b => b.ProductId == p.Id).ToList();
            
            int itemRemainingNeed = item.Quantity;
            
            var sortedBatches = productBatches
                .Where(b => b.ExpiryDate == null || b.ExpiryDate > DateTime.Now)
                .OrderBy(b => b.ExpiryDate == null)
                .ThenBy(b => b.ExpiryDate)
                .ToList();

            foreach (var batch in sortedBatches)
            {
                if (itemRemainingNeed <= 0) break;

                var allocatedQuantity = Math.Min(itemRemainingNeed, batch.RemainingQuantity);
                
                batch.RemainingQuantity -= allocatedQuantity;
                await _stockBatchRepository.UpdateAsync(batch);

                item.InvoiceItemBatches.Add(new InvoiceItemBatch
                {
                    StockBatchId = batch.Id,
                    Quantity = allocatedQuantity,
                    CostPrice = batch.ImportPrice
                });

                p.StockQuantity -= allocatedQuantity;
                await _productRepository.UpdateAsync(p);

                await _inventoryLogRepository.InsertAsync(new InventoryLog
                {
                    ProductId = p.Id,
                    UserId = AbpSession.UserId,
                    Type = InventoryLogType.Export,
                    Quantity = allocatedQuantity,
                    RemainingQuantity = p.StockQuantity,
                    StockBatchId = batch.Id,
                    ExpiryDate = batch.ExpiryDate,
                    SupplierId = batch.SupplierId,
                    ReferenceId = invoiceId,
                    ReferenceType = nameof(Invoice),
                    Note = $"Bán hàng - HĐ {invoice.InvoiceNumber} (Lô: {batch.BatchCode})"
                });

                itemRemainingNeed -= allocatedQuantity;
            }
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        return ObjectMapper.Map<InvoiceDto>(invoice);
    }

    [AbpAuthorize(PermissionNames.Pages_Invoices_Cancel)]
    [Abp.Domain.Uow.UnitOfWork(System.Transactions.IsolationLevel.Serializable)]
    public async Task CancelAsync(CancelInvoiceDto input)
    {
        var invoice = await _invoiceRepository.GetAll()
            .Include(x => x.InvoiceItems)
                .ThenInclude(x => x.InvoiceItemBatches)
            .FirstOrDefaultAsync(x => x.Id == input.Id);

        if (invoice == null)
        {
            throw new Abp.Domain.Entities.EntityNotFoundException(typeof(Invoice), input.Id);
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new UserFriendlyException("Hóa đơn này đã được hủy trước đó.");
        }

        // Check time limit for cancellation (within 24 hours)
        if (invoice.CreationTime < DateTime.Now.AddDays(-1))
        {
            throw new UserFriendlyException("Hóa đơn đã quá hạn 24 giờ, không thể hủy.");
        }

        // Update status and reason
        invoice.Status = InvoiceStatus.Cancelled;
        invoice.CancelReason = input.CancelReason;
        await _invoiceRepository.UpdateAsync(invoice);

        // Refund stock and write logs
        foreach (var item in invoice.InvoiceItems)
        {
            var p = await _productRepository.FirstOrDefaultAsync(item.ProductId);
            if (p == null) continue;

            if (item.InvoiceItemBatches != null && item.InvoiceItemBatches.Any())
            {
                foreach (var ib in item.InvoiceItemBatches)
                {
                    var batch = await _stockBatchRepository.FirstOrDefaultAsync(ib.StockBatchId);
                    if (batch != null)
                    {
                        batch.RemainingQuantity += ib.Quantity;
                        await _stockBatchRepository.UpdateAsync(batch);
                    }

                    p.StockQuantity += ib.Quantity;
                    await _productRepository.UpdateAsync(p);

                    await _inventoryLogRepository.InsertAsync(new InventoryLog
                    {
                        ProductId = p.Id,
                        UserId = AbpSession.UserId,
                        Type = InventoryLogType.Import,
                        Quantity = ib.Quantity,
                        RemainingQuantity = p.StockQuantity,
                        StockBatchId = batch?.Id,
                        ExpiryDate = batch?.ExpiryDate,
                        SupplierId = batch?.SupplierId,
                        ReferenceId = invoice.Id,
                        ReferenceType = nameof(Invoice),
                        Note = $"Hủy hóa đơn {invoice.InvoiceNumber} (Hoàn kho Lô: {batch?.BatchCode}) - Lý do: {input.CancelReason}"
                    });
                }
            }
            else
            {
                p.StockQuantity += item.Quantity;
                await _productRepository.UpdateAsync(p);

                await _inventoryLogRepository.InsertAsync(new InventoryLog
                {
                    ProductId = p.Id,
                    UserId = AbpSession.UserId,
                    Type = InventoryLogType.Import,
                    Quantity = item.Quantity,
                    RemainingQuantity = p.StockQuantity,
                    ReferenceId = invoice.Id,
                    ReferenceType = nameof(Invoice),
                    Note = $"Hủy hóa đơn {invoice.InvoiceNumber} - Lý do: {input.CancelReason}"
                });
            }
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    public async Task<InvoiceDashboardStatsDto> GetDashboardStatsAsync()
    {
        var query = _invoiceRepository.GetAll();

        var totalCount = await query.CountAsync();
        var completedCount = await query.CountAsync(x => x.Status == InvoiceStatus.Completed);
        var cancelledCount = await query.CountAsync(x => x.Status == InvoiceStatus.Cancelled);

        var totalRevenue = await query
            .Where(x => x.Status == InvoiceStatus.Completed)
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        return new InvoiceDashboardStatsDto
        {
            TotalInvoiceCount = totalCount,
            CompletedInvoiceCount = completedCount,
            CancelledInvoiceCount = cancelledCount,
            TotalRevenue = totalRevenue
        };
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var todayStr = DateTime.Today.ToString("yyyyMMdd");
        var prefix = $"HD-{todayStr}-";

        var count = await _invoiceRepository.GetAll()
            .IgnoreQueryFilters()
            .Where(x => x.InvoiceNumber.StartsWith(prefix))
            .CountAsync();

        return $"{prefix}{(count + 1):D4}";
    }
}
