using System;

namespace InternProject.Grocery.Categories.Dto;

public class CategoryDashboardStatsDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}
