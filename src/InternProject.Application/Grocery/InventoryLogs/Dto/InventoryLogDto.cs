using Abp.Application.Services.Dto;
using InternProject.Grocery;
using System;

namespace InternProject.Grocery.InventoryLogs.Dto;

public class InventoryLogDto : FullAuditedEntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public long? UserId { get; set; }
    public string? UserName { get; set; }
    public InventoryLogType Type { get; set; }
    public int Quantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal? UnitCostAtTime { get; set; }
    public string? BatchCode { get; set; }
    public Guid? StockBatchId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? Note { get; set; }
}
