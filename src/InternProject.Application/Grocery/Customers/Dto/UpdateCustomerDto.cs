using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;

namespace InternProject.Grocery.Customers.Dto;

[AutoMapTo(typeof(Customer))]
public class UpdateCustomerDto : CustomerDtoBase, IEntityDto<Guid>
{
    public Guid Id { get; set; }
}
