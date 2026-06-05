using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.Customers.Dto;

public abstract class CustomerDtoBase
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(32)]
    public string? Phone { get; set; }

    [StringLength(512)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}
