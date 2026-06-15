using System.Collections.Generic;

namespace InternProject.Grocery.Reports.Dto
{
    public class DashboardOverviewDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal TodayProfit { get; set; }
        public int TodayInvoicesCount { get; set; }
        public int LowStockProductsCount { get; set; }
        public int OutOfStockProductsCount { get; set; }
        
        public List<RecentInvoiceDto> RecentInvoices { get; set; }
        public List<MonthlyRevenueDto> MonthlyRevenueData { get; set; }
    }

    public class RecentInvoiceDto
    {
        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public System.DateTime CreationTime { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } // e.g. "2026-06"
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
    }
}
