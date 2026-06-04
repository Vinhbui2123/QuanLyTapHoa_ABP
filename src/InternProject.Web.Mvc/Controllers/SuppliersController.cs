using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using InternProject.Authorization;
using InternProject.Controllers;
using InternProject.Grocery.Suppliers;
using InternProject.Web.Models.Suppliers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InternProject.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Suppliers)]
public class SuppliersController : InternProjectControllerBase
{
    private readonly ISupplierAppService _supplierAppService;

    public SuppliersController(ISupplierAppService supplierAppService)
    {
        _supplierAppService = supplierAppService;
    }

    public ActionResult Index()
    {
        return View();
    }

    public async Task<ActionResult> EditModal(Guid supplierId)
    {
        var supplier = await _supplierAppService.GetAsync(new EntityDto<Guid>(supplierId));
        var model = new EditSupplierModalViewModel
        {
            Supplier = supplier
        };

        return PartialView("_EditModal", model);
    }
}
