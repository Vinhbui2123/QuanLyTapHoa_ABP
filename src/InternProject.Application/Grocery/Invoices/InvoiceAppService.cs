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
    // Service này xử lý nghiệp vụ bán hàng: tạo hóa đơn, kiểm tra thanh toán,
    // trừ tồn kho theo lô, ghi sổ kho và hoàn kho khi hủy hóa đơn.
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

    /*
    Khách mua hàng
        ↓
    1. Kiểm tra sản phẩm có tồn tại? còn bán không?
    2. Kiểm tra kho đủ không? có hết hạn không?
    3. Tính tổng tiền
    4. Kiểm tra tiền khách đưa đủ không?
    5. Tạo hóa đơn
    6. Trừ kho (lô nào trước? → hạn gần trước)
    7. Ghi nhật ký xuất kho
    */

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
        // Query danh sách hóa đơn cho DataTables: lọc theo từ khóa, phương thức thanh toán, trạng thái.
        IQueryable<Invoice> query = _invoiceRepository.GetAll()
            .Include(x => x.Customer)
            .Include(x => x.CashierUser);

        query = query.WhereIf(
            !input.Keyword.IsNullOrWhiteSpace(),
            x => x.InvoiceNumber.Contains(input.Keyword) ||
                 (x.Customer != null && x.Customer.Name.Contains(input.Keyword)) ||
                 (x.CashierUser != null && x.CashierUser.UserName.Contains(input.Keyword))
        );

        query = query.WhereIf(
            input.PaymentMethod.HasValue,
            x => x.PaymentMethod == input.PaymentMethod.Value
        );

        query = query.WhereIf(
            input.Status.HasValue,
            x => x.Status == input.Status.Value
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
        // Serializable giúp hạn chế rủi ro 2 thu ngân cùng bán vượt quá số lượng tồn tại cùng thời điểm.
        if (input.InvoiceItems == null || !input.InvoiceItems.Any())
        {
            throw new UserFriendlyException(L("InvoiceMustHaveItem"));
        }

        if (!Enum.IsDefined(typeof(PaymentMethod), input.PaymentMethod))
        {
            throw new UserFriendlyException(L("InvalidPaymentMethod"));
        }

        if (input.InvoiceItems.Any(x => x.Quantity <= 0))
        {
            throw new UserFriendlyException(L("QuantityMustBePositive"));
        }

        // Một sản phẩm có thể được click nhiều lần ở POS hoặc gửi lặp từ client.
        // Gộp trước khi kiểm tra tồn để tổng số lượng không thể vượt kho.
        var requestedItems = input.InvoiceItems
            .GroupBy(x => x.ProductId)
            .Select(group => new CreateInvoiceItemDto
            {
                ProductId = group.Key,
                Quantity = checked(group.Sum(x => x.Quantity))
            })
            .ToList();

        // Load một lần toàn bộ sản phẩm trong hóa đơn để tránh query lặp từng dòng.
        var productIds = requestedItems.Select(x => x.ProductId).ToList();
        var products = await _productRepository.GetAll()
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        // Lấy các lô còn tồn của những sản phẩm cần bán. Lô hết tồn không tham gia kiểm tra/xuất kho.
        var batches = await _stockBatchRepository.GetAll()
            .Where(b => productIds.Contains(b.ProductId) && b.RemainingQuantity > 0)
            .ToListAsync();

        // Vừa kiểm tra điều kiện bán, vừa tạo snapshot InvoiceItem để giữ lại tên/giá tại thời điểm bán.
        var items = new List<InvoiceItem>();
        decimal totalAmount = 0;

        foreach (var line in requestedItems)
        {
            var p = products.FirstOrDefault(x => x.Id == line.ProductId)
                ?? throw new UserFriendlyException(L("ProductNotFoundOrInactive"));

            if (line.Quantity <= 0)
            {
                throw new UserFriendlyException(L("ProductQuantityMustBePositive", p.Name));
            }

            var productBatches = batches.Where(b => b.ProductId == p.Id).ToList();

            // Tách riêng tổng tồn và tồn còn hạn để báo lỗi đúng nguyên nhân: thiếu hàng hay hàng đã hết hạn.
            var availableUnexpiredStock = productBatches
                .Where(b => b.ExpiryDate == null || b.ExpiryDate > DateTime.Now)
                .Sum(b => b.RemainingQuantity);

            var totalStock = productBatches.Sum(b => b.RemainingQuantity);

            if (line.Quantity > totalStock)
            {
                throw new UserFriendlyException(L("ProductOutOfStockWarning", p.Name));
            }

            if (line.Quantity > availableUnexpiredStock)
            {
                throw new UserFriendlyException(L("ProductExpiredWarning", p.Name));
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

        // Kiểm tra tiền khách đưa sau khi đã tính tổng tiền từ giá bán hiện tại.
        if (input.AmountPaid < totalAmount)
        {
            throw new UserFriendlyException(L("InsufficientAmount"));
        }

        // Tạo hóa đơn ở trạng thái Completed vì POS thanh toán xong mới ghi nhận hóa đơn.
        var invoice = new Invoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            CustomerId = input.CustomerId,
            CashierUserId = AbpSession.UserId ?? throw new UserFriendlyException(L("LoggedInUserNotFound")),
            TotalAmount = totalAmount,
            AmountPaid = input.AmountPaid,
            ChangeAmount = input.AmountPaid - totalAmount,
            PaymentMethod = input.PaymentMethod,
            Status = InvoiceStatus.Completed,
            Note = input.Note,
            InvoiceItems = items
        };

        var invoiceId = await _invoiceRepository.InsertAndGetIdAsync(invoice);

        // Sau khi có Id hóa đơn, trừ từng lô và ghi log tham chiếu về hóa đơn đó.
        foreach (var item in items)
        {
            var p = products.First(x => x.Id == item.ProductId);
            var productBatches = batches.Where(b => b.ProductId == p.Id).ToList();
            
            int itemRemainingNeed = item.Quantity;
            
            // FEFO: lô có hạn dùng gần nhất xuất trước; lô không có hạn dùng đưa xuống sau.
            var sortedBatches = productBatches
                .Where(b => b.ExpiryDate == null || b.ExpiryDate > DateTime.Now)
                .OrderBy(b => b.ExpiryDate == null)
                .ThenBy(b => b.ExpiryDate)
                .ToList();

            foreach (var batch in sortedBatches)
            {
                if (itemRemainingNeed <= 0) break;

                var allocatedQuantity = Math.Min(itemRemainingNeed, batch.RemainingQuantity);
                
                // Trừ tồn ở cấp lô.
                batch.RemainingQuantity -= allocatedQuantity;
                await _stockBatchRepository.UpdateAsync(batch);

                // Lưu hóa đơn đã lấy bao nhiêu từ lô nào, giá vốn bao nhiêu để tính lợi nhuận chính xác.
                item.InvoiceItemBatches.Add(new InvoiceItemBatch
                {
                    StockBatchId = batch.Id,
                    Quantity = allocatedQuantity,
                    CostPrice = batch.ImportPrice
                });

                // Đồng bộ tồn tổng trên Product để danh sách sản phẩm/POS đọc nhanh.
                p.StockQuantity -= allocatedQuantity;
                await _productRepository.UpdateAsync(p);

                // InventoryLog là sổ kho: mỗi lần xuất từ một lô đều có một dòng log.
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
        // Khi hủy phải load cả InvoiceItemBatches để biết trước đó hóa đơn đã trừ từ lô nào.
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
            throw new UserFriendlyException(L("InvoiceAlreadyCancelled"));
        }

        // Chỉ cho hủy trong 24 giờ để tránh hoàn kho các giao dịch quá cũ làm sai báo cáo.
        if (invoice.CreationTime < DateTime.Now.AddDays(-1))
        {
            throw new UserFriendlyException(L("InvoiceCancellationWindowExpired"));
        }

        // Hóa đơn không bị xóa; chỉ đổi trạng thái để vẫn giữ lịch sử bán hàng và lý do hủy.
        invoice.Status = InvoiceStatus.Cancelled;
        invoice.CancelReason = input.CancelReason;
        await _invoiceRepository.UpdateAsync(invoice);

        // Hoàn kho đúng các lô đã bị trừ khi bán; đồng thời ghi log nhập hoàn.
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
                // Nhánh dự phòng cho dữ liệu cũ chưa lưu InvoiceItemBatches.
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
        // Mã hóa đơn theo ngày, ví dụ HD-20260702-0001.
        var todayStr = DateTime.Today.ToString("yyyyMMdd");
        var prefix = $"HD-{todayStr}-";

        var count = await _invoiceRepository.GetAll()
            .IgnoreQueryFilters()
            .Where(x => x.InvoiceNumber.StartsWith(prefix))
            .CountAsync();

        return $"{prefix}{(count + 1):D4}";
    }
}
