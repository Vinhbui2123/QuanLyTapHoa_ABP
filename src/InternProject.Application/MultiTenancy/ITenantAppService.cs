using Abp.Application.Services;
using InternProject.MultiTenancy.Dto;

namespace InternProject.MultiTenancy;

public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
{
}

