using Abp.Application.Services.Dto;

namespace InternProject.Grocery.Categories.Dto;

public class PagedCategoryResultRequestDto : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }

    public bool? IsActive { get; set; }
}
