using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace InternProject.Web.Views;

public abstract class InternProjectRazorPage<TModel> : AbpRazorPage<TModel>
{
    [RazorInject]
    public IAbpSession AbpSession { get; set; }

    protected InternProjectRazorPage()
    {
        LocalizationSourceName = InternProjectConsts.LocalizationSourceName;
    }
}
