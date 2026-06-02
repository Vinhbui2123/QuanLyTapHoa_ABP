using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.Customers.Dto;

[AutoMapTo(typeof(Customer))]
public class UpdateCustomerDto : EntityDto<Guid>
{
    [StringLength(50)]
    public string? Code { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(32)]
    public string? Phone { get; set; }

    [StringLength(512)]
    public string? Address { get; set; }

    public bool IsActive { get; set; }
}
