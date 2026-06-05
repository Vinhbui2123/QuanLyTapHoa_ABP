using Abp.AspNetCore.Mvc.Authorization;
using InternProject.Authorization;
using InternProject.Controllers;
using InternProject.Grocery.StockBatches;
using InternProject.Grocery.Products;
using InternProject.Grocery.Products.Dto;
using InternProject.Grocery.Suppliers;
using InternProject.Grocery.Suppliers.Dto;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InternProject.Web.Controllers
{
    [AbpMvcAuthorize(PermissionNames.Pages_StockBatches)]
    public class StockBatchesController : InternProjectControllerBase
    {
        private readonly IProductAppService _productAppService;
        private readonly ISupplierAppService _supplierAppService;

        public StockBatchesController(
            IProductAppService productAppService,
            ISupplierAppService supplierAppService)
        {
            _productAppService = productAppService;
            _supplierAppService = supplierAppService;
        }

        public async Task<ActionResult> Index()
        {
            var products = await _productAppService.GetListAsync(new PagedProductResultRequestDto { MaxResultCount = 1000, IsActive = true });
            var suppliers = await _supplierAppService.GetListAsync(new PagedSupplierResultRequestDto { MaxResultCount = 1000, IsActive = true });
            
            ViewBag.Products = products.Items;
            ViewBag.Suppliers = suppliers.Items;
            
            return View();
        }
    }
}
