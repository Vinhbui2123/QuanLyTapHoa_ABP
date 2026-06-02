using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.Suppliers.Dto;

[AutoMapTo(typeof(Supplier))]
public class UpdateSupplierDto : EntityDto<Guid>
{
    [StringLength(50)]
    public string? Code { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(32)]
    public string? Phone { get; set; }

    [StringLength(256)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    public bool IsActive { get; set; }
}
