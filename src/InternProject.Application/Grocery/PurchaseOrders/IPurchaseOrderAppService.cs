using Abp.Application.Services;
using Abp.Application.Services.Dto;
using InternProject.Grocery.PurchaseOrders.Dto;
using System;
using System.Threading.Tasks;

namespace InternProject.Grocery.PurchaseOrders
{
    public interface IPurchaseOrderAppService : IApplicationService
    {
        Task<PurchaseOrderDto> GetAsync(EntityDto<Guid> input);
        Task<PagedResultDto<PurchaseOrderDto>> GetListAsync(PagedPurchaseOrderResultRequestDto input);
        Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto input);
    }
}
