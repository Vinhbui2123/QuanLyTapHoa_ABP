using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.Categories.Dto;

[AutoMapTo(typeof(Category))]
public class UpdateCategoryDto : EntityDto<Guid>
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
