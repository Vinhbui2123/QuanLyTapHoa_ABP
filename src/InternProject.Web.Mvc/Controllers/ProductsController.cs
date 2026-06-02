using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using InternProject.Authorization;
using InternProject.Controllers;
using InternProject.Grocery.Products;
using InternProject.Web.Models.Products;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InternProject.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Products)]
public class ProductsController : InternProjectControllerBase
{
    private readonly IProductAppService _productAppService;

    public ProductsController(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task<ActionResult> Index()
    {
        var categoryLookup = await _productAppService.GetCategoryLookupAsync();
        var model = new ProductListViewModel
        {
            Categories = categoryLookup.Items
        };
        return View(model);
    }

    public async Task<ActionResult> EditModal(Guid productId)
    {
        var product = await _productAppService.GetAsync(new EntityDto<Guid>(productId));
        var categoryLookup = await _productAppService.GetCategoryLookupAsync();
        var model = new EditProductModalViewModel
        {
            Product = product,
            Categories = categoryLookup.Items
        };

        return PartialView("_EditModal", model);
    }
}
