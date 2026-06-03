using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using System;

namespace InternProject.Grocery.Invoices.Dto;

[AutoMapFrom(typeof(InvoiceItem))]
public class InvoiceItemDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}
