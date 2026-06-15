using System;
using System.Collections.Generic;

namespace InternProject.Grocery.Reports.Dto
{
    public class InventoryReportDto
    {
        public decimal TotalStockValuation { get; set; }
        public int TotalItemsInStock { get; set; }
        public int ExpiringBatchesCount { get; set; }
        public int ExpiredBatchesCount { get; set; }

        public List<InventoryItemDetailDto> ProductStocks { get; set; }
        public List<ExpiringBatchDetailDto> ExpiringBatches { get; set; }
    }

    public class InventoryItemDetailDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public int StockQuantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal StockValuation { get; set; } // Quantity * CostPrice
        public string StockStatus { get; set; } // OutOfStock, LowStock, InStock
    }

    public class ExpiringBatchDetailDto
    {
        public string BatchCode { get; set; }
        public string ProductName { get; set; }
        public string SupplierName { get; set; }
        public int RemainingQuantity { get; set; }
        public decimal ImportPrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int DaysToExpiry { get; set; }
        public string Status { get; set; } // Expired, NearExpiry
    }
}
