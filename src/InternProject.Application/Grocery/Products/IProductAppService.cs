using Abp.Application.Services;
using Abp.Application.Services.Dto;
using InternProject.Grocery.Products.Dto;
using System;
using System.Threading.Tasks;

namespace InternProject.Grocery.Products;

public interface IProductAppService : IApplicationService
{
    Task<ProductDto> GetAsync(EntityDto<Guid> input);
    Task<PagedResultDto<ProductDto>> GetListAsync(PagedProductResultRequestDto input);
    Task CreateAsync(CreateUpdateProductDto input);
    Task UpdateAsync(UpdateProductDto input);
    Task DeleteAsync(EntityDto<Guid> input);
    Task<ListResultDto<CategoryLookupDto>> GetCategoryLookupAsync();
    Task<ProductDashboardStatsDto> GetDashboardStatsAsync();
}


