using Abp.Application.Services;
using Abp.Application.Services.Dto;
using InternProject.Grocery.Categories.Dto;
using System;
using System.Threading.Tasks;

namespace InternProject.Grocery.Categories;

public interface ICategoryAppService : IApplicationService
{
    Task<CategoryDto> GetAsync(EntityDto<Guid> input);
    Task<PagedResultDto<CategoryDto>> GetListAsync(PagedCategoryResultRequestDto input);
    Task CreateAsync(CreateUpdateCategoryDto input);
    Task UpdateAsync(UpdateCategoryDto input);
    Task DeleteAsync(EntityDto<Guid> input);
    Task<CategoryDashboardStatsDto> GetDashboardStatsAsync();
}
