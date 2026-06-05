using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;

namespace InternProject.Grocery.Products.Dto;

[AutoMapFrom(typeof(Product))]
public class ProductDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;

    public string? Sku { get; set; }

    public Guid? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? ImageUrl { get; set; }

    public decimal CostPrice { get; set; }

    public decimal SalePrice { get; set; }

    public int StockQuantity { get; set; }

    public int MinStock { get; set; }

    public string? Unit { get; set; }

    public bool IsActive { get; set; }

    public StockStatus StockStatus { get; set; }
}
