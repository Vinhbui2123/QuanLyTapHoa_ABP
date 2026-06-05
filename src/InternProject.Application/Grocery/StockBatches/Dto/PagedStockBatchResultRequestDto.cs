using Abp.Application.Services.Dto;
using System;

namespace InternProject.Grocery.StockBatches.Dto
{
    public class PagedStockBatchResultRequestDto : PagedAndSortedResultRequestDto
    {
        public string? Keyword { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? SupplierId { get; set; }
        public bool? IsExpiredOnly { get; set; }
    }
}
