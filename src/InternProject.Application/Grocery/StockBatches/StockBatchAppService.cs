using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using InternProject.Authorization;
using InternProject.Grocery.StockBatches.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace InternProject.Grocery.StockBatches
{
    [AbpAuthorize(PermissionNames.Pages_StockBatches)]
    public class StockBatchAppService : InternProjectAppServiceBase, IStockBatchAppService
    {
        private readonly IRepository<StockBatch, Guid> _stockBatchRepository;
        private readonly IRepository<Product, Guid> _productRepository;
        private readonly IRepository<InventoryLog, Guid> _inventoryLogRepository;

        public StockBatchAppService(
            IRepository<StockBatch, Guid> stockBatchRepository,
            IRepository<Product, Guid> productRepository,
            IRepository<InventoryLog, Guid> inventoryLogRepository)
        {
            _stockBatchRepository = stockBatchRepository;
            _productRepository = productRepository;
            _inventoryLogRepository = inventoryLogRepository;
        }

        public async Task<PagedResultDto<StockBatchDto>> GetListAsync(PagedStockBatchResultRequestDto input)
        {
            System.Linq.IQueryable<StockBatch> query = _stockBatchRepository.GetAll()
                .Include(x => x.Product)
                .Include(x => x.Supplier);

            query = query.WhereIf(
                !input.Keyword.IsNullOrWhiteSpace(),
                x => x.BatchCode.Contains(input.Keyword) ||
                     x.Product.Name.Contains(input.Keyword)
            )
            .WhereIf(input.ProductId.HasValue, x => x.ProductId == input.ProductId)
            .WhereIf(input.SupplierId.HasValue, x => x.SupplierId == input.SupplierId)
            .WhereIf(input.IsExpiredOnly == true, x => x.ExpiryDate.HasValue && x.ExpiryDate.Value < DateTime.Now);

            var totalCount = await query.CountAsync();

            if (!input.Sorting.IsNullOrWhiteSpace())
            {
                query = query.OrderBy(input.Sorting);
            }
            else
            {
                query = query.OrderByDescending(x => x.ExpiryDate)
                             .ThenByDescending(x => x.CreationTime);
            }

            var items = await query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToListAsync();

            return new PagedResultDto<StockBatchDto>(
                totalCount,
                ObjectMapper.Map<List<StockBatchDto>>(items)
            );
        }

        [AbpAuthorize(PermissionNames.Pages_StockBatches_Dispose)]
        [Abp.Domain.Uow.UnitOfWork(System.Transactions.IsolationLevel.Serializable)]
        public async Task DisposeBatchAsync(DisposeBatchInput input)
        {
            var batch = await _stockBatchRepository.FirstOrDefaultAsync(input.StockBatchId);
            if (batch == null)
            {
                throw new EntityNotFoundException(typeof(StockBatch), input.StockBatchId);
            }

            if (batch.RemainingQuantity <= 0)
            {
                throw new UserFriendlyException("Lô hàng này đã hết tồn kho, không thể hủy thêm.");
            }

            int qtyToDispose = input.Quantity ?? batch.RemainingQuantity;

            if (qtyToDispose <= 0)
            {
                throw new UserFriendlyException("Số lượng hủy phải lớn hơn 0.");
            }

            if (qtyToDispose > batch.RemainingQuantity)
            {
                throw new UserFriendlyException($"Số lượng hủy ({qtyToDispose}) vượt quá số lượng tồn kho còn lại của lô ({batch.RemainingQuantity}).");
            }

            batch.RemainingQuantity -= qtyToDispose;
            await _stockBatchRepository.UpdateAsync(batch);

            var product = await _productRepository.GetAsync(batch.ProductId);
            product.StockQuantity = Math.Max(0, product.StockQuantity - qtyToDispose);
            await _productRepository.UpdateAsync(product);

            var note = string.IsNullOrWhiteSpace(input.Reason)
                ? $"Hủy hàng hết hạn sử dụng - Lô: {batch.BatchCode}"
                : $"Hủy hàng - Lô: {batch.BatchCode}. Lý do: {input.Reason}";

            await _inventoryLogRepository.InsertAsync(new InventoryLog
            {
                ProductId = product.Id,
                UserId = AbpSession.UserId,
                Type = InventoryLogType.Dispose,
                Quantity = qtyToDispose,
                RemainingQuantity = product.StockQuantity,
                StockBatchId = batch.Id,
                ExpiryDate = batch.ExpiryDate,
                SupplierId = batch.SupplierId,
                ReferenceId = batch.Id,
                ReferenceType = nameof(StockBatch),
                Note = note
            });

            await CurrentUnitOfWork.SaveChangesAsync();
        }
    }
}
