using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.Web.Models;
using InternProject.Authorization;
using InternProject.Controllers;
using InternProject.Grocery.Products;
using InternProject.Web.Models.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InternProject.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Products)]
public class ProductsController : InternProjectControllerBase
{
    private readonly IProductAppService _productAppService;
    private readonly IConfiguration _configuration;

    public ProductsController(IProductAppService productAppService, IConfiguration configuration)
    {
        _productAppService = productAppService;
        _configuration = configuration;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [DontWrapResult]
    public async Task<ActionResult> UploadImage(IFormFile file)
    {
        if (!PermissionChecker.IsGranted(PermissionNames.Pages_Products_Create) && 
            !PermissionChecker.IsGranted(PermissionNames.Pages_Products_Edit))
        {
            return StatusCode(StatusCodes.Status403Forbidden, L("YouAreNotAuthorizedToPerformThisAction"));
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(L("FormIsNotValidMessage"));
        }

        var maxSize = _configuration.GetValue<long>("App:MaxProductImageSize", 2097152); // fallback 2MB
        if (file.Length > maxSize)
        {
            return BadRequest(L("ImageSizeTooLarge"));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (Array.IndexOf(allowedExtensions, extension) < 0)
        {
            return BadRequest(L("InvalidImageFormat"));
        }

        try
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString("N") + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var imageUrl = "/uploads/products/" + uniqueFileName;
            return Json(new { success = true, imageUrl = imageUrl, fileName = file.FileName });
        }
        catch (Exception ex)
        {
            Logger.Error($"Upload product image failed. Error: {ex.Message}", ex);
            return StatusCode(StatusCodes.Status500InternalServerError, L("UploadFailed"));
        }
    }
}
