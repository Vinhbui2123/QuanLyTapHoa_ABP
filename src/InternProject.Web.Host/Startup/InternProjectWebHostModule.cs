using Abp.Modules;
using Abp.Reflection.Extensions;
using InternProject.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace InternProject.Web.Host.Startup
{
    [DependsOn(
       typeof(InternProjectWebCoreModule))]
    public class InternProjectWebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;

        public InternProjectWebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(InternProjectWebHostModule).GetAssembly());
        }
    }
}
