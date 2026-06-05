using Abp.Application.Services.Dto;
using System;

namespace InternProject.Grocery.PurchaseOrders.Dto
{
    public class PagedPurchaseOrderResultRequestDto : PagedAndSortedResultRequestDto
    {
        public string? Keyword { get; set; }
        public Guid? SupplierId { get; set; }
        public PurchaseOrderStatus? Status { get; set; }
    }
}
