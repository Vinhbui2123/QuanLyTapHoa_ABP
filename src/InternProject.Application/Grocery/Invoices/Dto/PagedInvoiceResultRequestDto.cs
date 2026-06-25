using Abp.Application.Services.Dto;
using InternProject.Grocery;

namespace InternProject.Grocery.Invoices.Dto;

public class PagedInvoiceResultRequestDto : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }

    public bool? IsActive { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    public InvoiceStatus? Status { get; set; }
}
