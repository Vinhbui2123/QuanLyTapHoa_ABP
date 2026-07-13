using Abp.UI;
using InternProject.Grocery;
using InternProject.Grocery.Invoices;
using InternProject.Grocery.Invoices.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace InternProject.Tests.Grocery;

public class InvoiceAppService_Tests : InternProjectTestBase
{
    private readonly IInvoiceAppService _invoiceAppService;

    public InvoiceAppService_Tests()
    {
        _invoiceAppService = Resolve<IInvoiceAppService>();
    }

    [Fact]
    public async Task CreateInvoice_WithDuplicateProductLines_ShouldValidateCombinedQuantity()
    {
        var productId = Guid.NewGuid();

        await UsingDbContextAsync(async context =>
        {
            await context.Products.AddAsync(new Product
            {
                Id = productId,
                Name = "Test product",
                SalePrice = 10,
                CostPrice = 5,
                StockQuantity = 10,
                IsActive = true
            });

            await context.StockBatches.AddAsync(new StockBatch
            {
                ProductId = productId,
                BatchCode = "TEST-BATCH",
                InitialQuantity = 10,
                RemainingQuantity = 10,
                ImportPrice = 5,
                ExpiryDate = DateTime.UtcNow.AddDays(30)
            });
        });

        Exception exception = null;
        try
        {
            await _invoiceAppService.CreateAsync(new CreateInvoiceDto
            {
                AmountPaid = 140,
                PaymentMethod = PaymentMethod.Cash,
                InvoiceItems = new List<CreateInvoiceItemDto>
                {
                    new() { ProductId = productId, Quantity = 7 },
                    new() { ProductId = productId, Quantity = 7 }
                }
            });
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (exception is not UserFriendlyException)
        {
            throw new Exception(exception?.ToString());
        }
        exception.ShouldBeOfType<UserFriendlyException>();

        await UsingDbContextAsync(async context =>
        {
            (await context.Invoices.CountAsync()).ShouldBe(0);
        });
    }
}
