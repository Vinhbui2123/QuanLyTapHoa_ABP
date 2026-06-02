using Abp.Application.Services;
using Abp.Application.Services.Dto;
using InternProject.Grocery.Suppliers.Dto;
using System;
using System.Threading.Tasks;

namespace InternProject.Grocery.Suppliers;

public interface ISupplierAppService : IApplicationService
{
    Task<SupplierDto> GetAsync(EntityDto<Guid> input);
    Task CreateAsync(CreateUpdateSupplierDto input);
    Task DeleteAsync(Guid id);
    Task UpdateAsync(UpdateSupplierDto input);

    Task<PagedResultDto<SupplierDto>> GetListAsync(PagedSupplierResultRequestDto input);

}
