using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using InternProject.Authorization;
using InternProject.Grocery.Products.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace InternProject.Grocery.Products;

[AbpAuthorize(PermissionNames.Pages_Products)]
public class ProductAppService : InternProjectAppServiceBase, IProductAppService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;

    public ProductAppService(
        IRepository<Product, Guid> productRepository,
        IRepository<Category, Guid> categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ProductDto> GetAsync(EntityDto<Guid> input)
    {
        var product = await _productRepository.GetAll()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == input.Id);
        
        if (product == null)
        {
            throw new Abp.Domain.Entities.EntityNotFoundException(typeof(Product), input.Id);
        }

        return ObjectMapper.Map<ProductDto>(product);
    }

    public async Task<PagedResultDto<ProductDto>> GetListAsync(PagedProductResultRequestDto input)
    {
        var query = _productRepository.GetAll()
            .Include(x => x.Category)
            .WhereIf(
                !input.Keyword.IsNullOrWhiteSpace(),
                x => x.Name.Contains(input.Keyword) ||
                     (x.Sku != null && x.Sku.Contains(input.Keyword))
            )
            .WhereIf(input.CategoryId.HasValue, x => x.CategoryId == input.CategoryId)
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

        var products = await query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();

        return new PagedResultDto<ProductDto>(
            totalCount,
            ObjectMapper.Map<List<ProductDto>>(products)
        );
    }

    [AbpAuthorize(PermissionNames.Pages_Products_Create)]
    public async Task CreateAsync(CreateUpdateProductDto input)
    {
        var product = ObjectMapper.Map<Product>(input);
        await _productRepository.InsertAsync(product);
    }

    [AbpAuthorize(PermissionNames.Pages_Products_Edit)]
    public async Task UpdateAsync(UpdateProductDto input)
    {
        var product = await _productRepository.GetAsync(input.Id);
        ObjectMapper.Map(input, product);
        await _productRepository.UpdateAsync(product);
    }

    [AbpAuthorize(PermissionNames.Pages_Products_Delete)]
    public async Task DeleteAsync(EntityDto<Guid> input)
    {
        await _productRepository.DeleteAsync(input.Id);
    }

    public async Task<ListResultDto<CategoryLookupDto>> GetCategoryLookupAsync()
    {
        var categories = await _categoryRepository.GetAll()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return new ListResultDto<CategoryLookupDto>(
            ObjectMapper.Map<List<CategoryLookupDto>>(categories)
        );
    }

    public async Task<ProductDashboardStatsDto> GetDashboardStatsAsync()
    {
        var query = _productRepository.GetAll();
        return new ProductDashboardStatsDto
        {
            TotalCount = await query.CountAsync(),
            ActiveCount = await query.CountAsync(x => x.IsActive),
            LowStockCount = await query.CountAsync(x => x.StockQuantity > 0 && x.StockQuantity <= x.MinStock),
            OutOfStockCount = await query.CountAsync(x => x.StockQuantity <= 0)
        };
    }
}

