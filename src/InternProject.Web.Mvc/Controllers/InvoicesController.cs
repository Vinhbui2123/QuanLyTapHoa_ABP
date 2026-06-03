using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using InternProject.Authorization;
using InternProject.Controllers;
using InternProject.Grocery.Invoices;
using InternProject.Grocery.Invoices.Dto;
using InternProject.Grocery.Customers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InternProject.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Invoices)]
public class InvoicesController : InternProjectControllerBase
{
    private readonly IInvoiceAppService _invoiceAppService;
    private readonly ICustomerAppService _customerAppService;

    public InvoicesController(
        IInvoiceAppService invoiceAppService,
        ICustomerAppService customerAppService)
    {
        _invoiceAppService = invoiceAppService;
        _customerAppService = customerAppService;
    }

    public async Task<ActionResult> Index()
    {
        return View();
    }

    [AbpMvcAuthorize(PermissionNames.Pages_Invoices_Create)]
    public async Task<ActionResult> Create()
    {
        var customers = await _customerAppService.GetListAsync(new Grocery.Customers.Dto.PagedCustomerResultRequestDto { MaxResultCount = 1000, IsActive = true });
        ViewBag.Customers = customers.Items;
        return View();
    }

    public async Task<ActionResult> DetailModal(Guid invoiceId)
    {
        var invoice = await _invoiceAppService.GetAsync(new EntityDto<Guid>(invoiceId));
        return PartialView("_DetailModal", invoice);
    }
}
