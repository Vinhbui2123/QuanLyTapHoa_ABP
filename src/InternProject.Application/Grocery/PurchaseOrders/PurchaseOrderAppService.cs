using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using InternProject.Authorization;
using InternProject.Grocery.PurchaseOrders.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace InternProject.Grocery.PurchaseOrders
{
    [AbpAuthorize(PermissionNames.Pages_PurchaseOrders)]
    public class PurchaseOrderAppService : InternProjectAppServiceBase, IPurchaseOrderAppService
    {
        private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
        private readonly IRepository<PurchaseOrderItem, Guid> _purchaseOrderItemRepository;
        private readonly IRepository<Product, Guid> _productRepository;
        private readonly IRepository<StockBatch, Guid> _stockBatchRepository;
        private readonly IRepository<InventoryLog, Guid> _inventoryLogRepository;

        public PurchaseOrderAppService(
            IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
            IRepository<PurchaseOrderItem, Guid> purchaseOrderItemRepository,
            IRepository<Product, Guid> productRepository,
            IRepository<StockBatch, Guid> stockBatchRepository,
            IRepository<InventoryLog, Guid> inventoryLogRepository)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
            _purchaseOrderItemRepository = purchaseOrderItemRepository;
            _productRepository = productRepository;
            _stockBatchRepository = stockBatchRepository;
            _inventoryLogRepository = inventoryLogRepository;
        }

        public async Task<PurchaseOrderDto> GetAsync(EntityDto<Guid> input)
        {
            var order = await _purchaseOrderRepository.GetAll()
                .Include(x => x.Supplier)
                .Include(x => x.User)
                .Include(x => x.PurchaseOrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(x => x.Id == input.Id);

            if (order == null)
            {
                throw new EntityNotFoundException(typeof(PurchaseOrder), input.Id);
            }

            return MapToDto(order);
        }

        public async Task<PagedResultDto<PurchaseOrderDto>> GetListAsync(PagedPurchaseOrderResultRequestDto input)
        {
            System.Linq.IQueryable<PurchaseOrder> query = _purchaseOrderRepository.GetAll()
                .Include(x => x.Supplier)
                .Include(x => x.User);

            query = query.WhereIf(
                !input.Keyword.IsNullOrWhiteSpace(),
                x => x.OrderNumber.Contains(input.Keyword) ||
                     x.Supplier.Name.Contains(input.Keyword) ||
                     x.User.UserName.Contains(input.Keyword)
            )
            .WhereIf(input.SupplierId.HasValue, x => x.SupplierId == input.SupplierId)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status);

            var totalCount = await query.CountAsync();

            if (!input.Sorting.IsNullOrWhiteSpace())
            {
                query = query.OrderBy(input.Sorting);
            }
            else
            {
                query = query.OrderByDescending(x => x.CreationTime);
            }

            var orders = await query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToListAsync();

            return new PagedResultDto<PurchaseOrderDto>(
                totalCount,
                orders.Select(MapToDto).ToList()
            );
        }

        [AbpAuthorize(PermissionNames.Pages_PurchaseOrders_Create)]
        [Abp.Domain.Uow.UnitOfWork(System.Transactions.IsolationLevel.Serializable)]
        public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto input)
        {
            if (input.PurchaseOrderItems == null || !input.PurchaseOrderItems.Any())
            {
                throw new UserFriendlyException("Phiếu nhập phải có ít nhất 1 mặt hàng.");
            }

            var order = new PurchaseOrder
            {
                OrderNumber = await GenerateOrderNumberAsync(),
                SupplierId = input.SupplierId,
                UserId = AbpSession.UserId ?? throw new UserFriendlyException("Không tìm thấy thông tin người dùng đăng nhập."),
                TotalAmount = input.PurchaseOrderItems.Sum(x => x.Quantity * x.UnitPrice),
                Status = PurchaseOrderStatus.Completed,
                Note = input.Note,
                PurchaseOrderItems = new List<PurchaseOrderItem>()
            };

            foreach (var item in input.PurchaseOrderItems)
            {
                order.PurchaseOrderItems.Add(new PurchaseOrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Subtotal = item.Quantity * item.UnitPrice,
                    BatchId = item.BatchId,
                    ExpiryDate = item.ExpiryDate
                });
            }

            var orderId = await _purchaseOrderRepository.InsertAndGetIdAsync(order);
            await CurrentUnitOfWork.SaveChangesAsync();

            foreach (var item in order.PurchaseOrderItems)
            {
                var batchCode = string.IsNullOrWhiteSpace(item.BatchId)
                    ? $"BATCH-{order.OrderNumber}-{item.ProductId.ToString().Substring(0, 8).ToUpper()}"
                    : item.BatchId;

                var stockBatch = new StockBatch
                {
                    ProductId = item.ProductId,
                    SupplierId = order.SupplierId,
                    PurchaseOrderItemId = item.Id,
                    BatchCode = batchCode,
                    ExpiryDate = item.ExpiryDate,
                    ImportPrice = item.UnitPrice,
                    InitialQuantity = item.Quantity,
                    RemainingQuantity = item.Quantity
                };

                var batchId = await _stockBatchRepository.InsertAndGetIdAsync(stockBatch);

                var product = await _productRepository.GetAsync(item.ProductId);
                product.StockQuantity += item.Quantity;
                await _productRepository.UpdateAsync(product);

                await _inventoryLogRepository.InsertAsync(new InventoryLog
                {
                    ProductId = product.Id,
                    UserId = order.UserId,
                    Type = InventoryLogType.Import,
                    Quantity = item.Quantity,
                    RemainingQuantity = product.StockQuantity,
                    StockBatchId = batchId,
                    ExpiryDate = item.ExpiryDate,
                    SupplierId = order.SupplierId,
                    ReferenceId = orderId,
                    ReferenceType = nameof(PurchaseOrder),
                    Note = $"Nhập hàng - Phiếu nhập {order.OrderNumber} (Lô: {batchCode})"
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            return ObjectMapper.Map<PurchaseOrderDto>(order);
        }

        private async Task<string> GenerateOrderNumberAsync()
        {
            var todayStr = DateTime.Today.ToString("yyyyMMdd");
            var prefix = $"PO-{todayStr}-";

            var count = await _purchaseOrderRepository.GetAll()
                .IgnoreQueryFilters()
                .Where(x => x.OrderNumber.StartsWith(prefix))
                .CountAsync();

            return $"{prefix}{(count + 1):D4}";
        }

        private static PurchaseOrderDto MapToDto(PurchaseOrder order)
        {
            return new PurchaseOrderDto
            {
                Id = order.Id,
                CreationTime = order.CreationTime,
                CreatorUserId = order.CreatorUserId,
                LastModificationTime = order.LastModificationTime,
                LastModifierUserId = order.LastModifierUserId,
                OrderNumber = order.OrderNumber,
                SupplierId = order.SupplierId,
                SupplierName = order.Supplier?.Name,
                UserId = order.UserId,
                UserName = order.User?.UserName,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Note = order.Note,
                PurchaseOrderItems = order.PurchaseOrderItems?
                    .Select(item => new PurchaseOrderItemDto
                    {
                        Id = item.Id,
                        PurchaseOrderId = item.PurchaseOrderId,
                        ProductId = item.ProductId,
                        ProductName = item.Product?.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal,
                        BatchId = item.BatchId,
                        ExpiryDate = item.ExpiryDate
                    })
                    .ToList() ?? new List<PurchaseOrderItemDto>()
            };
        }
    }
}
