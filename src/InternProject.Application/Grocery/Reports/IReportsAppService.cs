using Abp.Application.Services;
using InternProject.Grocery.Reports.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InternProject.Grocery.Reports
{
    public interface IReportsAppService : IApplicationService
    {
        Task<DashboardOverviewDto> GetDashboardOverviewAsync();
        Task<RevenueReportDto> GetRevenueReportAsync(GetRevenueReportInput input);
        Task<InventoryReportDto> GetInventoryReportAsync(GetInventoryReportInput input);
        Task<List<TopSellingProductDto>> GetTopSellingProductsReportAsync(GetTopSellingProductsInput input);
    }
}
