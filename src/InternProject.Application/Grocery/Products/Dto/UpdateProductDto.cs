using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.Products.Dto;

[AutoMapTo(typeof(Product))]
public class UpdateProductDto : EntityDto<Guid>
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Sku { get; set; }

    public Guid? CategoryId { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public decimal CostPrice { get; set; }

    public decimal SalePrice { get; set; }

    public int StockQuantity { get; set; }

    public int MinStock { get; set; } = 10;

    [StringLength(20)]
    public string? Unit { get; set; }

    public bool IsActive { get; set; } = true;
}
