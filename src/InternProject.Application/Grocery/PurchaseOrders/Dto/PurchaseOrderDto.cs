using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;
using System.Collections.Generic;

namespace InternProject.Grocery.PurchaseOrders.Dto
{
    [AutoMapFrom(typeof(PurchaseOrder))]
    public class PurchaseOrderDto : AuditedEntityDto<Guid>
    {
        public string OrderNumber { get; set; }

        public Guid SupplierId { get; set; }

        public string SupplierName { get; set; }

        public long UserId { get; set; }

        public string UserName { get; set; }

        public decimal TotalAmount { get; set; }

        public PurchaseOrderStatus Status { get; set; }

        public string? Note { get; set; }

        public List<PurchaseOrderItemDto> PurchaseOrderItems { get; set; } = new();
    }
}
