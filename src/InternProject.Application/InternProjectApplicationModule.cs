using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using InternProject.Authorization;

namespace InternProject;

[DependsOn(
    typeof(InternProjectCoreModule),
    typeof(AbpAutoMapperModule))]
public class InternProjectApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Authorization.Providers.Add<InternProjectAuthorizationProvider>();
    }

    public override void Initialize()
    {
        var thisAssembly = typeof(InternProjectApplicationModule).GetAssembly();

        IocManager.RegisterAssemblyByConvention(thisAssembly);

        Configuration.Modules.AbpAutoMapper().Configurators.Add(
            // Scan the assembly for classes which inherit from AutoMapper.Profile
            cfg => cfg.AddMaps(thisAssembly)
        );
    }
}
