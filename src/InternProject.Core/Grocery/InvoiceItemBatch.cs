using Abp.Domain.Entities.Auditing;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternProject.Grocery
{
    public class InvoiceItemBatch : FullAuditedEntity<Guid>
    {
        [Required]
        public Guid InvoiceItemId { get; set; }

        [ForeignKey(nameof(InvoiceItemId))]
        public virtual InvoiceItem InvoiceItem { get; set; }

        [Required]
        public Guid StockBatchId { get; set; }

        [ForeignKey(nameof(StockBatchId))]
        public virtual StockBatch StockBatch { get; set; }

        public int Quantity { get; set; }

        public decimal CostPrice { get; set; }
    }
}
