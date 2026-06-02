using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using InternProject.Authorization;
using InternProject.Controllers;
using InternProject.Grocery.Categories;
using InternProject.Web.Models.Categories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InternProject.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Categories)]
public class CategoriesController : InternProjectControllerBase
{
    private readonly ICategoryAppService _categoryAppService;

    public CategoriesController(ICategoryAppService categoryAppService)
    {
        _categoryAppService = categoryAppService;
    }

    public ActionResult Index()
    {
        return View();
    }

    public async Task<ActionResult> EditModal(Guid categoryId)
    {
        var category = await _categoryAppService.GetAsync(new EntityDto<Guid>(categoryId));
        var model = new EditCategoryModalViewModel
        {
            Category = category
        };

        return PartialView("_EditModal", model);
    }
}
