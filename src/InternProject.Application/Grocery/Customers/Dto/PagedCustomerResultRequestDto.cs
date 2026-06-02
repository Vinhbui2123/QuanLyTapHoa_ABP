using Abp.Application.Services.Dto;

namespace InternProject.Grocery.Customers.Dto;

public class PagedCustomerResultRequestDto : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }

    public bool? IsActive { get; set; }
}
