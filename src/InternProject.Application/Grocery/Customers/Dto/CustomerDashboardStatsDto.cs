using System;

namespace InternProject.Grocery.Customers.Dto;

public class CustomerDashboardStatsDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}
