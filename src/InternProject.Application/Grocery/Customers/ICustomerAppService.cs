using Abp.Application.Services;
using Abp.Application.Services.Dto;
using InternProject.Grocery.Customers.Dto;
using System;
using System.Threading.Tasks;

namespace InternProject.Grocery.Customers;

public interface ICustomerAppService : IApplicationService
{
    Task<CustomerDto> GetAsync(EntityDto<Guid> input);
    Task<PagedResultDto<CustomerDto>> GetListAsync(PagedCustomerResultRequestDto input);
    Task CreateAsync(CreateCustomerDto input);
    Task UpdateAsync(UpdateCustomerDto input);
    Task DeleteAsync(EntityDto<Guid> input);
    Task<CustomerDashboardStatsDto> GetDashboardStatsAsync();
}
