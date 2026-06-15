using System.Collections.Generic;

namespace InternProject.Grocery.Reports.Dto
{
    public class RevenueReportDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMarginPercent { get; set; }
        public int TotalInvoices { get; set; }
        public int TotalCancelledInvoices { get; set; }

        public List<RevenueChartPointDto> ChartPoints { get; set; }
        public List<RevenueInvoiceDetailDto> Invoices { get; set; }
    }

    public class RevenueChartPointDto
    {
        public string TimeLabel { get; set; } // e.g. "2026-06-15" or "2026-06"
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
    }

    public class RevenueInvoiceDetailDto
    {
        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public string CashierName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public string PaymentMethod { get; set; }
        public System.DateTime CreationTime { get; set; }
    }
}
