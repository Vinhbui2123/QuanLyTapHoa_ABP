using Abp.Authorization;
using Abp.Runtime.Session;
using InternProject.Configuration.Dto;
using System.Threading.Tasks;

namespace InternProject.Configuration;

[AbpAuthorize]
public class ConfigurationAppService : InternProjectAppServiceBase, IConfigurationAppService
{
    public async Task ChangeUiTheme(ChangeUiThemeInput input)
    {
        await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
    }
}
