using Abp.AspNetCore.Mvc.Authorization;
using InternProject.Authorization;
using InternProject.Controllers;
using InternProject.Grocery.Products;
using InternProject.Grocery.Products.Dto;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InternProject.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_InventoryLogs)]
public class InventoryLogsController : InternProjectControllerBase
{
    private readonly IProductAppService _productAppService;

    public InventoryLogsController(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task<ActionResult> Index()
    {
        var products = await _productAppService.GetListAsync(new PagedProductResultRequestDto
        {
            MaxResultCount = 1000,
            IsActive = null
        });

        ViewBag.Products = products.Items;
        return View();
    }
}
