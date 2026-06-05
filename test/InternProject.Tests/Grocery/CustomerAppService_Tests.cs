using Abp.Application.Services.Dto;
using InternProject.Grocery;
using InternProject.Grocery.Customers;
using InternProject.Grocery.Customers.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace InternProject.Tests.Grocery;

public class CustomerAppService_Tests : InternProjectTestBase
{
    private readonly ICustomerAppService _customerAppService;

    public CustomerAppService_Tests()
    {
        _customerAppService = Resolve<ICustomerAppService>();
    }

    [Fact]
    public async Task CreateCustomer_Test()
    {
        const string customerName = "Nguyen Van A";

        await _customerAppService.CreateAsync(new CreateCustomerDto
        {
            Name = customerName,
            Phone = "0909000111",
            Address = "HCM",
            IsActive = true
        });

        await UsingDbContextAsync(async context =>
        {
            var customer = await context.Customers.FirstOrDefaultAsync(x => x.Name == customerName);
            customer.ShouldNotBeNull();
            customer.Id.ShouldNotBe(Guid.Empty);
            customer.Phone.ShouldBe("0909000111");
            customer.Address.ShouldBe("HCM");
        });
    }

    [Fact]
    public async Task UpdateAndDeleteCustomer_Test()
    {
        var customerId = Guid.NewGuid();

        await UsingDbContextAsync(async context =>
        {
            await context.Customers.AddAsync(new Customer
            {
                Id = customerId,
                Name = "Initial Customer",
                Phone = "0123",
                Address = "Initial Address",
                IsActive = true
            });

            await context.SaveChangesAsync();
        });

        await _customerAppService.UpdateAsync(new UpdateCustomerDto
        {
            Id = customerId,
            Name = "Updated Customer",
            Phone = "0999",
            Address = "Updated Address",
            IsActive = false
        });

        var updatedCustomer = await _customerAppService.GetAsync(new EntityDto<Guid>(customerId));
        updatedCustomer.Name.ShouldBe("Updated Customer");
        updatedCustomer.IsActive.ShouldBeFalse();

        await _customerAppService.DeleteAsync(new EntityDto<Guid>(customerId));

        await UsingDbContextAsync(async context =>
        {
            var customer = await context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == customerId);
            customer.ShouldNotBeNull();
            customer.IsDeleted.ShouldBeTrue();
        });
    }
}
