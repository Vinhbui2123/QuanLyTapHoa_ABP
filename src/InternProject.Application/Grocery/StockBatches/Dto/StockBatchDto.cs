using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;

namespace InternProject.Grocery.StockBatches.Dto
{
    [AutoMapFrom(typeof(StockBatch))]
    public class StockBatchDto : FullAuditedEntityDto<Guid>
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductSku { get; set; }

        public Guid? SupplierId { get; set; }

        public string SupplierName { get; set; }

        public Guid? PurchaseOrderItemId { get; set; }

        public string BatchCode { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public decimal ImportPrice { get; set; }

        public int InitialQuantity { get; set; }

        public int RemainingQuantity { get; set; }
    }
}
