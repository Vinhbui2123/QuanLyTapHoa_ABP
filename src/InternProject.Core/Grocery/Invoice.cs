using Abp.Domain.Entities.Auditing;
using InternProject.Authorization.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternProject.Grocery
{
    public class Invoice : FullAuditedEntity<Guid>
    {
        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; }

        public Guid? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [Required]
        public long CashierUserId { get; set; }

        [ForeignKey(nameof(CashierUserId))]
        public virtual User CashierUser { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal AmountPaid { get; set; }

        public decimal ChangeAmount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        [StringLength(500)]
        public string? Note { get; set; }

        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; }
    }
}
