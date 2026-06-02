using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;

namespace InternProject.Grocery.Products.Dto;

[AutoMapFrom(typeof(Category))]
public class CategoryLookupDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
}

