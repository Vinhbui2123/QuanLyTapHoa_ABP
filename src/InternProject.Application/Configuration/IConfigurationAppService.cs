using InternProject.Configuration.Dto;
using System.Threading.Tasks;

namespace InternProject.Configuration;

public interface IConfigurationAppService
{
    Task ChangeUiTheme(ChangeUiThemeInput input);
}
