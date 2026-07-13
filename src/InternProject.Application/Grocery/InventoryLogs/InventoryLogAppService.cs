using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using InternProject.Authorization;
using InternProject.Grocery.InventoryLogs.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace InternProject.Grocery.InventoryLogs;

[AbpAuthorize(PermissionNames.Pages_InventoryLogs)]
public class InventoryLogAppService : InternProjectAppServiceBase, IInventoryLogAppService
{
    private readonly IRepository<InventoryLog, Guid> _inventoryLogRepository;

    public InventoryLogAppService(IRepository<InventoryLog, Guid> inventoryLogRepository)
    {
        _inventoryLogRepository = inventoryLogRepository;
    }

    public async Task<PagedResultDto<InventoryLogDto>> GetListAsync(PagedInventoryLogResultRequestDto input)
    {
        // Nhật ký là dữ liệu truy vết nên vẫn phải xem được khi sản phẩm đã ngừng/xóa mềm.
        var query = _inventoryLogRepository.GetAll().IgnoreQueryFilters()
            .Include(x => x.Product)
            .Include(x => x.User)
            .Include(x => x.Supplier)
            .Include(x => x.StockBatch)
            .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x =>
                x.Product.Name.Contains(input.Keyword) ||
                (x.Product.Sku != null && x.Product.Sku.Contains(input.Keyword)) ||
                (x.BatchId != null && x.BatchId.Contains(input.Keyword)) ||
                (x.ReferenceType != null && x.ReferenceType.Contains(input.Keyword)))
            .WhereIf(input.ProductId.HasValue, x => x.ProductId == input.ProductId.Value)
            .WhereIf(input.Type.HasValue, x => x.Type == input.Type.Value)
            .WhereIf(input.FromDate.HasValue, x => x.CreationTime >= input.FromDate.Value.Date)
            .WhereIf(input.ToDate.HasValue, x => x.CreationTime < input.ToDate.Value.Date.AddDays(1));

        var totalCount = await query.CountAsync();
        query = string.IsNullOrWhiteSpace(input.Sorting)
            ? query.OrderByDescending(x => x.CreationTime)
            : query.OrderBy(input.Sorting);

        var logs = await query.Skip(input.SkipCount).Take(input.MaxResultCount).ToListAsync();
        var result = logs.Select(x => new InventoryLogDto
        {
            Id = x.Id,
            CreationTime = x.CreationTime,
            CreatorUserId = x.CreatorUserId,
            LastModificationTime = x.LastModificationTime,
            LastModifierUserId = x.LastModifierUserId,
            ProductId = x.ProductId,
            ProductName = x.Product?.Name ?? string.Empty,
            ProductSku = x.Product?.Sku,
            UserId = x.UserId,
            UserName = x.User?.UserName,
            Type = x.Type,
            Quantity = x.Quantity,
            RemainingQuantity = x.RemainingQuantity,
            UnitCostAtTime = x.UnitCostAtTime,
            BatchCode = x.StockBatch?.BatchCode ?? x.BatchId,
            StockBatchId = x.StockBatchId,
            ExpiryDate = x.ExpiryDate,
            SupplierId = x.SupplierId,
            SupplierName = x.Supplier?.Name,
            ReferenceId = x.ReferenceId,
            ReferenceType = x.ReferenceType,
            Note = x.Note
        }).ToList();

        return new PagedResultDto<InventoryLogDto>(totalCount, result);
    }
}
