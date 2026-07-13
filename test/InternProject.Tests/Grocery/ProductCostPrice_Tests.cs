using Abp.Application.Services.Dto;
using InternProject.Grocery;
using InternProject.Grocery.Products;
using InternProject.Grocery.Products.Dto;
using InternProject.Grocery.PurchaseOrders;
using InternProject.Grocery.PurchaseOrders.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InternProject.Tests.Grocery;

public class ProductCostPrice_Tests : InternProjectTestBase
{
    private readonly IProductAppService _productAppService;
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public ProductCostPrice_Tests()
    {
        _productAppService = Resolve<IProductAppService>();
        _purchaseOrderAppService = Resolve<IPurchaseOrderAppService>();
    }

    [Fact]
    public async Task ImportingProduct_ShouldUpdateLatestCostPriceAndStock()
    {
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        await UsingDbContextAsync(async context =>
        {
            await context.Products.AddAsync(new Product
            {
                Id = productId,
                Name = "Imported product",
                CostPrice = 0,
                SalePrice = 10_000,
                StockQuantity = 0,
                IsActive = true
            });
            await context.Suppliers.AddAsync(new Supplier
            {
                Id = supplierId,
                Code = "SUP-COST-TEST",
                Name = "Cost price supplier",
                IsActive = true
            });
        });

        await _purchaseOrderAppService.CreateAsync(new CreatePurchaseOrderDto
        {
            SupplierId = supplierId,
            PurchaseOrderItems = new List<CreatePurchaseOrderItemDto>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 100,
                    UnitPrice = 7_000,
                    BatchId = "COST-TEST-BATCH"
                }
            }
        });

        await UsingDbContextAsync(async context =>
        {
            var product = await context.Products.SingleAsync(x => x.Id == productId);
            product.StockQuantity.ShouldBe(100);
            product.CostPrice.ShouldBe(7_000);
        });
    }

    [Fact]
    public async Task GetProduct_ShouldUseLatestBatchPriceForExistingImportedData()
    {
        var productId = Guid.NewGuid();

        await UsingDbContextAsync(async context =>
        {
            await context.Products.AddAsync(new Product
            {
                Id = productId,
                Name = "Legacy imported product",
                CostPrice = 0,
                SalePrice = 10_000,
                StockQuantity = 5,
                IsActive = true
            });
            await context.StockBatches.AddAsync(new StockBatch
            {
                ProductId = productId,
                BatchCode = "LEGACY-COST-BATCH",
                ImportPrice = 6_500,
                InitialQuantity = 5,
                RemainingQuantity = 5
            });
        });

        var product = await _productAppService.GetAsync(new EntityDto<Guid>(productId));

        product.CostPrice.ShouldBe(6_500);
    }

    [Fact]
    public async Task UpdatingProduct_ShouldKeepStockAndCostButAllowMinStockChange()
    {
        var productId = Guid.NewGuid();

        await UsingDbContextAsync(async context =>
        {
            await context.Products.AddAsync(new Product
            {
                Id = productId,
                Name = "Editable product",
                CostPrice = 6_500,
                SalePrice = 10_000,
                StockQuantity = 5,
                MinStock = 10,
                IsActive = true
            });
        });

        await _productAppService.UpdateAsync(new UpdateProductDto
        {
            Id = productId,
            Name = "Editable product",
            CostPrice = 1,
            SalePrice = 11_000,
            StockQuantity = 99,
            MinStock = 2,
            IsActive = true
        });

        await UsingDbContextAsync(async context =>
        {
            var product = await context.Products.SingleAsync(x => x.Id == productId);
            product.StockQuantity.ShouldBe(5);
            product.CostPrice.ShouldBe(6_500);
            product.MinStock.ShouldBe(2);
        });
    }
}
