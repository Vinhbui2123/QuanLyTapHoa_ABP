using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;

namespace InternProject.Grocery.Suppliers.Dto;

[AutoMapFrom(typeof(Supplier))]
public class SupplierDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? ContactPerson { get; set; }

    public bool IsActive { get; set; }
}
