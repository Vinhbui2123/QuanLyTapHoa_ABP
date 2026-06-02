using System;

namespace InternProject.Grocery.Products.Dto;

public class ProductDashboardStatsDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
}
