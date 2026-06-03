using System;
using System.Collections.Generic;
using InternProject.Grocery;

namespace InternProject.Grocery.Invoices.Dto;

public class CreateInvoiceDto
{
    public Guid? CustomerId { get; set; }
    public decimal AmountPaid { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Note { get; set; }
    public List<CreateInvoiceItemDto> InvoiceItems { get; set; } = new();
}
