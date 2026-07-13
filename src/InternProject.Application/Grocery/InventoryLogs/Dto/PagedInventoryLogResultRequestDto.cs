using Abp.Application.Services.Dto;
using InternProject.Grocery;
using System;

namespace InternProject.Grocery.InventoryLogs.Dto;

public class PagedInventoryLogResultRequestDto : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }
    public Guid? ProductId { get; set; }
    public InventoryLogType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
