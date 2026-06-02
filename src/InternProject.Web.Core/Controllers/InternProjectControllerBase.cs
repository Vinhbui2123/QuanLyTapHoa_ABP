using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace InternProject.Controllers
{
    public abstract class InternProjectControllerBase : AbpController
    {
        protected InternProjectControllerBase()
        {
            LocalizationSourceName = InternProjectConsts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
