using System;

namespace InternProject.Grocery.Invoices.Dto;

public class CreateInvoiceItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
