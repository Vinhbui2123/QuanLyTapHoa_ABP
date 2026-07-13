using Abp.Application.Services;
using Abp.Application.Services.Dto;
using InternProject.Grocery.InventoryLogs.Dto;
using System;
using System.Threading.Tasks;

namespace InternProject.Grocery.InventoryLogs;

public interface IInventoryLogAppService : IApplicationService
{
    Task<PagedResultDto<InventoryLogDto>> GetListAsync(PagedInventoryLogResultRequestDto input);
}
