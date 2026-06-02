using Abp.Application.Services.Dto;

namespace InternProject.Grocery.Suppliers.Dto;

public class PagedSupplierResultRequestDto : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }

    public bool? IsActive { get; set; }
}
