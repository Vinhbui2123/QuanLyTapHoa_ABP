using Abp.Modules;
using Abp.Reflection.Extensions;
using InternProject.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace InternProject.Web.Startup;

[DependsOn(typeof(InternProjectWebCoreModule))]
public class InternProjectWebMvcModule : AbpModule
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfigurationRoot _appConfiguration;

    public InternProjectWebMvcModule(IWebHostEnvironment env)
    {
        _env = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public override void PreInitialize()
    {
        Configuration.Navigation.Providers.Add<InternProjectNavigationProvider>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(InternProjectWebMvcModule).GetAssembly());
    }
}
