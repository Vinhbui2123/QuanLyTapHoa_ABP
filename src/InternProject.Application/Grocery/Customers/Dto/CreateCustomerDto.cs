using Abp.AutoMapper;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.Customers.Dto;

[AutoMapTo(typeof(Customer))]
public class CreateCustomerDto : CustomerDtoBase
{
    [StringLength(50)]
    public string? Code { get; set; }
}
