using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using InternProject.Authorization;
using InternProject.Controllers;
using InternProject.Grocery.PurchaseOrders;
using InternProject.Grocery.PurchaseOrders.Dto;
using InternProject.Grocery.Suppliers;
using InternProject.Grocery.Suppliers.Dto;
using InternProject.Grocery.Products;
using InternProject.Grocery.Products.Dto;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InternProject.Web.Controllers
{
    [AbpMvcAuthorize(PermissionNames.Pages_PurchaseOrders)]
    public class PurchaseOrdersController : InternProjectControllerBase
    {
        private readonly IPurchaseOrderAppService _purchaseOrderAppService;
        private readonly ISupplierAppService _supplierAppService;
        private readonly IProductAppService _productAppService;

        public PurchaseOrdersController(
            IPurchaseOrderAppService purchaseOrderAppService,
            ISupplierAppService supplierAppService,
            IProductAppService productAppService)
        {
            _purchaseOrderAppService = purchaseOrderAppService;
            _supplierAppService = supplierAppService;
            _productAppService = productAppService;
        }

        public async Task<ActionResult> Index()
        {
            var suppliers = await _supplierAppService.GetListAsync(new PagedSupplierResultRequestDto { MaxResultCount = 1000, IsActive = true });
            ViewBag.Suppliers = suppliers.Items;
            return View();
        }

        [AbpMvcAuthorize(PermissionNames.Pages_PurchaseOrders_Create)]
        public async Task<ActionResult> Create()
        {
            var suppliers = await _supplierAppService.GetListAsync(new PagedSupplierResultRequestDto { MaxResultCount = 1000, IsActive = true });
            var products = await _productAppService.GetListAsync(new PagedProductResultRequestDto { MaxResultCount = 1000, IsActive = true });
            
            ViewBag.Suppliers = suppliers.Items;
            ViewBag.Products = products.Items;
            
            return View();
        }

        public async Task<ActionResult> DetailModal(Guid orderId)
        {
            var order = await _purchaseOrderAppService.GetAsync(new EntityDto<Guid>(orderId));
            return PartialView("_DetailModal", order);
        }
    }
}
