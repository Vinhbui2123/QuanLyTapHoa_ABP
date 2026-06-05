using Abp.Domain.Entities.Auditing;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternProject.Grocery
{
    public class StockBatch : FullAuditedEntity<Guid>
    {
        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public Guid? SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        public Guid? PurchaseOrderItemId { get; set; }

        [ForeignKey(nameof(PurchaseOrderItemId))]
        public virtual PurchaseOrderItem? PurchaseOrderItem { get; set; }

        [Required]
        [StringLength(50)]
        public string BatchCode { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public decimal ImportPrice { get; set; }

        public int InitialQuantity { get; set; }

        public int RemainingQuantity { get; set; }
    }
}
