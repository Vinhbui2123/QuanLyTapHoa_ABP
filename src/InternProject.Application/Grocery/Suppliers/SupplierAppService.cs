using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using InternProject.Authorization;
using InternProject.Grocery.Suppliers.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace InternProject.Grocery.Suppliers;

[AbpAuthorize(PermissionNames.Pages_Suppliers)]
public class SupplierAppService : InternProjectAppServiceBase, ISupplierAppService
{
    private readonly IRepository<Supplier, Guid> _supplierRepository;

    public SupplierAppService(IRepository<Supplier, Guid> supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierDto> GetAsync(EntityDto<Guid> input)
    {
        var supplier = await _supplierRepository.GetAsync(input.Id);
        return ObjectMapper.Map<SupplierDto>(supplier);
    }
    public async Task<PagedResultDto<SupplierDto>> GetListAsync(PagedSupplierResultRequestDto input)
    {
        var query = _supplierRepository.GetAll()
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

        var suppliers = await query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();

        return new PagedResultDto<SupplierDto>(
            totalCount,
            ObjectMapper.Map<List<SupplierDto>>(suppliers)
        );
    }
    
    [AbpAuthorize(PermissionNames.Pages_Suppliers_Create)]
    public async Task CreateAsync(CreateUpdateSupplierDto input)
    {
        var supplier = ObjectMapper.Map<Supplier>(input);
        await _supplierRepository.InsertAsync(supplier);
    }

    [AbpAuthorize(PermissionNames.Pages_Suppliers_Edit)]
    public async Task UpdateAsync(UpdateSupplierDto input)
    {
        var supplier = await _supplierRepository.GetAsync(input.Id);
        ObjectMapper.Map(input, supplier);
        await _supplierRepository.UpdateAsync(supplier);
    }   
    [AbpAuthorize(PermissionNames.Pages_Suppliers_Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _supplierRepository.DeleteAsync(id);
    }
}
