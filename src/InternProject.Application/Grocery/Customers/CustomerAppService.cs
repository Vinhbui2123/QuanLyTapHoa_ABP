using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using InternProject.Authorization;
using InternProject.Grocery.Customers.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace InternProject.Grocery.Customers;

[AbpAuthorize(PermissionNames.Pages_Customers)]
public class CustomerAppService : InternProjectAppServiceBase, ICustomerAppService
{
    private readonly IRepository<Customer, Guid> _customerRepository;

    public CustomerAppService(IRepository<Customer, Guid> customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto> GetAsync(EntityDto<Guid> input)
    {
        var customer = await _customerRepository.GetAsync(input.Id);
        return ObjectMapper.Map<CustomerDto>(customer);
    }

    public async Task<PagedResultDto<CustomerDto>> GetListAsync(PagedCustomerResultRequestDto input)
    {
        var query = _customerRepository.GetAll()
            .WhereIf(
                !input.Keyword.IsNullOrWhiteSpace(),
                x => x.Name.Contains(input.Keyword) ||
                     (x.Code != null && x.Code.Contains(input.Keyword)) ||
                     (x.Phone != null && x.Phone.Contains(input.Keyword)) ||
                     (x.Address != null && x.Address.Contains(input.Keyword)))
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);

        var totalCount = await query.CountAsync();

        if (!input.Sorting.IsNullOrWhiteSpace())
        {
            query = query.OrderBy(input.Sorting);
        }
        else
        {
            query = query.OrderByDescending(x => x.CreationTime);
        }

        var customers = await query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();

        return new PagedResultDto<CustomerDto>(
            totalCount,
            ObjectMapper.Map<List<CustomerDto>>(customers)
        );
    }

    [AbpAuthorize(PermissionNames.Pages_Customers_Create)]
    public async Task CreateAsync(CreateUpdateCustomerDto input)
    {
        var customer = ObjectMapper.Map<Customer>(input);
        await _customerRepository.InsertAsync(customer);
    }

    [AbpAuthorize(PermissionNames.Pages_Customers_Edit)]
    public async Task UpdateAsync(UpdateCustomerDto input)
    {
        var customer = await _customerRepository.GetAsync(input.Id);
        ObjectMapper.Map(input,customer);
        await _customerRepository.UpdateAsync(customer);
    }

    [AbpAuthorize(PermissionNames.Pages_Customers_Delete)]
    public async Task DeleteAsync(EntityDto<Guid> input)
    {
        await _customerRepository.DeleteAsync(input.Id);
    }

    public async Task<CustomerDashboardStatsDto> GetDashboardStatsAsync()
    {
        var query = _customerRepository.GetAll();
        return new CustomerDashboardStatsDto
        {
            TotalCount = await query.CountAsync(),
            ActiveCount = await query.CountAsync(x => x.IsActive),
            InactiveCount = await query.CountAsync(x => !x.IsActive)
        };
    }
}
