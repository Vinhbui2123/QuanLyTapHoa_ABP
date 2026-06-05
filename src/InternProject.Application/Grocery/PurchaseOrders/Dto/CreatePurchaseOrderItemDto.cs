using System;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.PurchaseOrders.Dto
{
    public class CreatePurchaseOrderItemDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [StringLength(50)]
        public string? BatchId { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
