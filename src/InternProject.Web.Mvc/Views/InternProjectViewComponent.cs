using Abp.AspNetCore.Mvc.ViewComponents;

namespace InternProject.Web.Views;

public abstract class InternProjectViewComponent : AbpViewComponent
{
    protected InternProjectViewComponent()
    {
        LocalizationSourceName = InternProjectConsts.LocalizationSourceName;
    }
}
