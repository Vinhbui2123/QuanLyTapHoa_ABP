using Abp.Application.Services.Dto;
using System;

namespace InternProject.Grocery.Products.Dto;

public class PagedProductResultRequestDto : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }

    public Guid? CategoryId { get; set; }

    public bool? IsActive { get; set; }
}
