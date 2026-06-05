using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;

namespace InternProject.Grocery.Customers.Dto;

[AutoMapFrom(typeof(Customer))]
public class CustomerDto : EntityDto<Guid>
{
    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }
}
