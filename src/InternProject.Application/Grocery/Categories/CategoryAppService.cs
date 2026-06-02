using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using InternProject.Authorization;
using InternProject.Grocery.Categories.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace InternProject.Grocery.Categories;

[AbpAuthorize(PermissionNames.Pages_Categories)]
public class CategoryAppService : InternProjectAppServiceBase, ICategoryAppService
{
    private readonly IRepository<Category, Guid> _categoryRepository;

    public CategoryAppService(IRepository<Category, Guid> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto> GetAsync(EntityDto<Guid> input)
    {
        var category = await _categoryRepository.GetAsync(input.Id);
        return ObjectMapper.Map<CategoryDto>(category);
    }

    public async Task<PagedResultDto<CategoryDto>> GetListAsync(PagedCategoryResultRequestDto input)
    {
        var query = _categoryRepository.GetAll()
            .WhereIf(
                !input.Keyword.IsNullOrWhiteSpace(),
                x => x.Name.Contains(input.Keyword) ||
                     (x.Description != null && x.Description.Contains(input.Keyword))
            )
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

        var categories = await query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();

        return new PagedResultDto<CategoryDto>(
            totalCount,
            ObjectMapper.Map<List<CategoryDto>>(categories)
        );
    }

    [AbpAuthorize(PermissionNames.Pages_Categories_Create)]
    public async Task CreateAsync(CreateUpdateCategoryDto input)
    {
        var category = ObjectMapper.Map<Category>(input);
        await _categoryRepository.InsertAsync(category);
    }

    [AbpAuthorize(PermissionNames.Pages_Categories_Edit)]
    public async Task UpdateAsync(UpdateCategoryDto input)
    {
        var category = await _categoryRepository.GetAsync(input.Id);
        ObjectMapper.Map(input, category);
        await _categoryRepository.UpdateAsync(category);
    }

    [AbpAuthorize(PermissionNames.Pages_Categories_Delete)]
    public async Task DeleteAsync(EntityDto<Guid> input)
    {
        await _categoryRepository.DeleteAsync(input.Id);
    }

    public async Task<CategoryDashboardStatsDto> GetDashboardStatsAsync()
    {
        var query = _categoryRepository.GetAll();
        return new CategoryDashboardStatsDto
        {
            TotalCount = await query.CountAsync(),
            ActiveCount = await query.CountAsync(x => x.IsActive),
            InactiveCount = await query.CountAsync(x => !x.IsActive)
        };
    }
}
