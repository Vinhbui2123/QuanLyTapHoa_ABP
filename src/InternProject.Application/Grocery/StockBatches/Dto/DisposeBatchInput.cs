using System;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.StockBatches.Dto
{
    public class DisposeBatchInput
    {
        [Required]
        public Guid StockBatchId { get; set; }

        public int? Quantity { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }
}
