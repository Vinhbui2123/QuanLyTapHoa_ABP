using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using InternProject.Authorization;
using InternProject.Controllers;
using InternProject.Grocery.Customers;
using InternProject.Web.Models.Customers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InternProject.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Customers)]
public class CustomersController : InternProjectControllerBase
{
    private readonly ICustomerAppService _customerAppService;

    public CustomersController(ICustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    public ActionResult Index()
    {
        return View();
    }

    public async Task<ActionResult> EditModal(Guid customerId)
    {
        var customer = await _customerAppService.GetAsync(new Abp.Application.Services.Dto.EntityDto<Guid>(customerId));
        var model = new EditCustomerModalViewModel
        {
            Customer = customer
        };

        return PartialView("_EditModal", model);
    }
}
