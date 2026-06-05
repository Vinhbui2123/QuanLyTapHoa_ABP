using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;

namespace InternProject.Grocery.PurchaseOrders.Dto
{
    [AutoMapFrom(typeof(PurchaseOrderItem))]
    public class PurchaseOrderItemDto : EntityDto<Guid>
    {
        public Guid PurchaseOrderId { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Subtotal { get; set; }

        public string? BatchId { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
