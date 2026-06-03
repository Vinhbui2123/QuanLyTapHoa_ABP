using Abp.Domain.Entities.Auditing;
using InternProject.Authorization.Users;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternProject.Grocery
{
    public class InventoryLog : FullAuditedEntity<Guid>
    {
        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public long? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        public InventoryLogType Type { get; set; }

        /// <summary>
        /// Số lượng thay đổi tuyệt đối (luôn dương).
        /// Hướng tăng/giảm xác định bởi Type:
        /// - Import: Cộng vào kho
        /// - Export: Trừ khỏi kho
        /// - Dispose: Trừ khỏi kho
        /// - Adjust: Tùy thuộc hướng điều chỉnh
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Tồn kho sau khi log này được ghi (running balance)
        /// </summary>
        public int RemainingQuantity { get; set; }

        public decimal? UnitCostAtTime { get; set; }

        [StringLength(50)]
        public string? BatchId { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public Guid? SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        public Guid? ReferenceId { get; set; }

        [StringLength(100)]
        public string? ReferenceType { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }
    }
}
