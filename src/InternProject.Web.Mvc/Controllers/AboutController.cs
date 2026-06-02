using Abp.AspNetCore.Mvc.Authorization;
using InternProject.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace InternProject.Web.Controllers;

[AbpMvcAuthorize]
public class AboutController : InternProjectControllerBase
{
    public ActionResult Index()
    {
        return View();
    }
}
