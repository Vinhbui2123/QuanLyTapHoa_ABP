using Abp.Application.Services;
using Abp.Application.Services.Dto;
using InternProject.Grocery.Invoices.Dto;
using System;
using System.Threading.Tasks;

namespace InternProject.Grocery.Invoices;

public interface IInvoiceAppService : IApplicationService
{
    Task<InvoiceDto> GetAsync(EntityDto<Guid> input);
    Task<PagedResultDto<InvoiceDto>> GetListAsync(PagedInvoiceResultRequestDto input);
    Task<InvoiceDto> CreateAsync(CreateInvoiceDto input);
    Task CancelAsync(CancelInvoiceDto input);
    Task<InvoiceDashboardStatsDto> GetDashboardStatsAsync();
}
