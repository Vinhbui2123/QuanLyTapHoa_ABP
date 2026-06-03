using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using InternProject.Grocery;
using System;
using System.Collections.Generic;

namespace InternProject.Grocery.Invoices.Dto;

[AutoMapFrom(typeof(Invoice))]
public class InvoiceDto : FullAuditedEntityDto<Guid>
{
    public string InvoiceNumber { get; set; }

    public Guid? CustomerId { get; set; }

    public string CustomerName { get; set; }

    public long CashierUserId { get; set; }

    public string CashierUserName { get; set; }

    public decimal TotalAmount { get; set; }
    
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public InvoiceStatus Status { get; set; }

    public string? CancelReason { get; set; }
    public string? Note { get; set; }

    public List<InvoiceItemDto> InvoiceItems { get; set; } = new();
}
