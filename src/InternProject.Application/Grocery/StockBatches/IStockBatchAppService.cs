using Abp.Application.Services;
using Abp.Application.Services.Dto;
using InternProject.Grocery.StockBatches.Dto;
using System;
using System.Threading.Tasks;

namespace InternProject.Grocery.StockBatches
{
    public interface IStockBatchAppService : IApplicationService
    {
        Task<PagedResultDto<StockBatchDto>> GetListAsync(PagedStockBatchResultRequestDto input);
        Task DisposeBatchAsync(DisposeBatchInput input);
    }
}
