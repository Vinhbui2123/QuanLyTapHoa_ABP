using Abp.Application.Services.Dto;

namespace InternProject.Grocery.Invoices.Dto;

public class PagedInvoiceResultRequestDto : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }

    public bool? IsActive { get; set; }
}
