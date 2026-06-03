using System;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.Invoices.Dto;

public class CancelInvoiceDto
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(500)]
    public string CancelReason { get; set; }
}
