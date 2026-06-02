using Abp.Application.Services;
using InternProject.Authorization.Accounts.Dto;
using System.Threading.Tasks;

namespace InternProject.Authorization.Accounts;

public interface IAccountAppService : IApplicationService
{
    Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

    Task<RegisterOutput> Register(RegisterInput input);
}
