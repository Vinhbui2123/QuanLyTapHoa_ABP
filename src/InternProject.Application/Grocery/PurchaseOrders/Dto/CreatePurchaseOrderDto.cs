using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.PurchaseOrders.Dto
{
    public class CreatePurchaseOrderDto
    {
        [Required]
        public Guid SupplierId { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [Required]
        public List<CreatePurchaseOrderItemDto> PurchaseOrderItems { get; set; } = new();
    }
}
