using HRMS.Models;
using System.Web.Mvc;

namespace HRMS.Filters
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var user = filterContext.HttpContext.Session["LoginInfo"] as UsersModel;

            if (user == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Login" }
                    }
                );

                return;
            }

            if (user.IsAdmin == false)
            {
                if (filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    filterContext.Result = new JsonResult
                    {
                        Data = new
                        {
                            success = false,
                            message = "Bạn không có quyền truy cập chức năng này."
                        },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
                else
                {
                    filterContext.Controller.TempData["ToastrType"] = "error";
                    filterContext.Controller.TempData["ToastrMessage"] = "Bạn không có quyền truy cập chức năng này.";

                    filterContext.Result = new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary
                        {
                            { "controller", "Home" },
                            { "action", "Index" }
                        }
                    );
                }

                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}