namespace InternProject.Grocery.Invoices.Dto;

public class InvoiceDashboardStatsDto
{
    public int TotalInvoiceCount { get; set; }
    public int CompletedInvoiceCount { get; set; }
    public int CancelledInvoiceCount { get; set; }
    public decimal TotalRevenue { get; set; }
}
