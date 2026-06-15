using Abp.AspNetCore.Mvc.Authorization;
using InternProject.Authorization;
using InternProject.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace InternProject.Web.Controllers
{
    [AbpMvcAuthorize(PermissionNames.Pages_Reports)]
    public class ReportsController : InternProjectControllerBase
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
