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
    // ProductAppService quản lý thông tin sản phẩm. Tồn kho không cho sửa trực tiếp ở đây,
    // mà được tăng/giảm qua nhập hàng, bán hàng, hủy lô để lịch sử kho luôn có dấu vết.
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<InvoiceItem, Guid> _invoiceItemRepository;
    private readonly IRepository<PurchaseOrderItem, Guid> _purchaseOrderItemRepository;
    private readonly IRepository<StockBatch, Guid> _stockBatchRepository;

    public ProductAppService(
        IRepository<Product, Guid> productRepository,
        IRepository<Category, Guid> categoryRepository,
        IRepository<InvoiceItem, Guid> invoiceItemRepository,
        IRepository<PurchaseOrderItem, Guid> purchaseOrderItemRepository,
        IRepository<StockBatch, Guid> stockBatchRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _invoiceItemRepository = invoiceItemRepository;
        _purchaseOrderItemRepository = purchaseOrderItemRepository;
        _stockBatchRepository = stockBatchRepository;
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

        var dto = ObjectMapper.Map<ProductDto>(product);
        await ApplyLatestImportPricesAsync(new List<ProductDto> { dto });
        return dto;
    }

    public async Task<PagedResultDto<ProductDto>> GetListAsync(PagedProductResultRequestDto input)
    {
        // Include Category để DTO/list có tên danh mục, không phải gọi thêm từng sản phẩm.
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

        var productDtos = ObjectMapper.Map<List<ProductDto>>(products);
        await ApplyLatestImportPricesAsync(productDtos);

        return new PagedResultDto<ProductDto>(totalCount, productDtos);
    }

    [AbpAuthorize(PermissionNames.Pages_Products_Create)]
    public async Task CreateAsync(CreateUpdateProductDto input)
    {
        input.Name = input.Name?.Trim() ?? string.Empty;
        input.Sku = input.Sku?.Trim();
        await ValidateProductAsync(input.Name, input.Sku, 0, input.SalePrice, input.MinStock, null);
        var product = ObjectMapper.Map<Product>(input);
        // Giá vốn không nhập ở danh mục sản phẩm; nguồn giá vốn thực tế nằm ở từng lô nhập kho.
        product.CostPrice = 0;
        // Sản phẩm mới luôn bắt đầu tồn kho = 0; tồn kho chỉ tăng khi lập phiếu nhập.
        product.StockQuantity = 0;
        await _productRepository.InsertAsync(product);
    }

    [AbpAuthorize(PermissionNames.Pages_Products_Edit)]
    public async Task UpdateAsync(UpdateProductDto input)
    {
        input.Name = input.Name?.Trim() ?? string.Empty;
        input.Sku = input.Sku?.Trim();
        var product = await _productRepository.GetAsync(input.Id);
        await ValidateProductAsync(input.Name, input.Sku, product.CostPrice, input.SalePrice, input.MinStock, input.Id);
        var oldImageUrl = product.ImageUrl;
        // Giữ lại tồn kho hiện tại để người dùng không thể sửa số lượng bằng form thông tin sản phẩm.
        var currentStockQuantity = product.StockQuantity;
        var currentCostPrice = product.CostPrice;

        ObjectMapper.Map(input, product);
        product.StockQuantity = currentStockQuantity;
        // Không nhận thay đổi giá vốn từ form/API cập nhật thông tin sản phẩm.
        product.CostPrice = currentCostPrice;
        await _productRepository.UpdateAsync(product);

        if (oldImageUrl != product.ImageUrl)
        {
            TryDeleteProductImage(oldImageUrl);
        }
    }

    [AbpAuthorize(PermissionNames.Pages_Products_Delete)]
    public async Task DeleteAsync(EntityDto<Guid> input)
    {
        var product = await _productRepository.FirstOrDefaultAsync(input.Id);
        if (product != null)
        {
            var hasTransactions = await _invoiceItemRepository.GetAll().AnyAsync(x => x.ProductId == input.Id)
                || await _purchaseOrderItemRepository.GetAll().AnyAsync(x => x.ProductId == input.Id);

            if (hasTransactions)
            {
                product.IsActive = false;
                await _productRepository.UpdateAsync(product);
                return;
            }

            var imageUrl = product.ImageUrl;
            await _productRepository.DeleteAsync(product);
            TryDeleteProductImage(imageUrl);
        }
    }

    private async Task ValidateProductAsync(
        string name,
        string? sku,
        decimal costPrice,
        decimal salePrice,
        int minStock,
        Guid? excludedId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new Abp.UI.UserFriendlyException(L("ProductNameRequired"));
        }

        if (costPrice < 0 || salePrice < 0 || minStock < 0)
        {
            throw new Abp.UI.UserFriendlyException(L("ProductValuesMustBeNonNegative"));
        }

        if (!string.IsNullOrWhiteSpace(sku))
        {
            var normalizedSku = sku.Trim();
            var duplicateSku = await _productRepository.GetAll()
                .AnyAsync(x => x.Sku == normalizedSku && (!excludedId.HasValue || x.Id != excludedId.Value));

            if (duplicateSku)
            {
                throw new Abp.UI.UserFriendlyException(L("ProductSkuAlreadyExists"));
            }
        }
    }

    private async Task ApplyLatestImportPricesAsync(List<ProductDto> products)
    {
        if (products.Count == 0)
        {
            return;
        }

        var productIds = products.Select(x => x.Id).ToList();
        var importedBatches = await _stockBatchRepository.GetAll()
            .Where(x => productIds.Contains(x.ProductId))
            .Select(x => new { x.ProductId, x.ImportPrice, x.CreationTime, x.Id })
            .ToListAsync();

        // Bù cho dữ liệu đã nhập trước khi Product.CostPrice được đồng bộ ở luồng nhập hàng.
        var latestPrices = importedBatches
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(b => b.CreationTime)
                    .ThenByDescending(b => b.Id)
                    .First()
                    .ImportPrice);

        foreach (var product in products)
        {
            if (latestPrices.TryGetValue(product.Id, out var latestPrice))
            {
                product.CostPrice = latestPrice;
            }
        }
    }

    private void TryDeleteProductImage(string imageUrl)
    {
        // Chỉ xóa file do hệ thống upload trong thư mục sản phẩm, tránh xóa nhầm URL ngoài.
        if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/uploads/products/"))
        {
            return;
        }

        try
        {
            var relativePath = imageUrl.TrimStart('/');
            var fileSystemPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", relativePath);
            if (System.IO.File.Exists(fileSystemPath))
            {
                System.IO.File.Delete(fileSystemPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to delete product image file: {imageUrl}. Error: {ex.Message}", ex);
        }
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

