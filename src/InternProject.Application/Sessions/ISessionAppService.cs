using Abp.Application.Services;
using InternProject.Sessions.Dto;
using System.Threading.Tasks;

namespace InternProject.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
}
