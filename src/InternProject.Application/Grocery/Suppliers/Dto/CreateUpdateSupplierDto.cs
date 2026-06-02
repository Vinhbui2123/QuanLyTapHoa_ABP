using Abp.AutoMapper;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.Suppliers.Dto;

[AutoMapTo(typeof(Supplier))]
public class CreateUpdateSupplierDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

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

    public bool IsActive { get; set; } = true;
}
