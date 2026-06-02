using Abp.Domain.Entities.Auditing;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternProject.Grocery
{
    public class Product : FullAuditedEntity<Guid>
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(50)]
        public string? Sku { get; set; }

        public Guid? CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public virtual Category? Category { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public decimal CostPrice { get; set; }

        public decimal SalePrice { get; set; }

        public int StockQuantity { get; set; }

        public int MinStock { get; set; } = 10;

        [StringLength(20)]
        public string? Unit { get; set; }

        public bool IsActive { get; set; } = true;

        public StockStatus StockStatus
        {
            get
            {
                if (StockQuantity <= 0) return StockStatus.OutOfStock;
                if (StockQuantity <= MinStock) return StockStatus.LowStock;
                return StockStatus.InStock;
            }
        }
    }
}
