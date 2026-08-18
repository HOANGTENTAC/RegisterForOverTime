using HRMS.Filters;
using HRMS.Helpers;
using HRMS.Models;
using System;
using System.Data;
using System.Web.Mvc;

namespace HRMS.Controllers
{
    [AdminAuthorize]
    public class NotificationsController : Controller
    {
        [HttpGet]
        public JsonResult PasswordResetSummary()
        {
            var user = Session["LoginInfo"] as UsersModel;

            // Chỉ Admin mới được xem notification này
            if (user == null || !user.IsAdmin)
            {
                return Json(new
                {
                    success = true,
                    count = 0,
                    url = ""
                }, JsonRequestBehavior.AllowGet);
            }

            const string sql = @"
                SELECT COUNT(1) AS Total
                FROM [TIME_KEEPING].[dbo].[Users]
                WHERE YeuCauCapLaiMatKhau = 1";

            DataTable dt = SQLHelper.ExecuteDt(sql);

            int total = 0;

            if (dt.Rows.Count > 0 &&
                dt.Rows[0]["Total"] != DBNull.Value)
            {
                total = Convert.ToInt32(dt.Rows[0]["Total"]);
            }

            return Json(new
            {
                success = true,
                count = total,
                url = Url.Action(
                    "Index",
                    "Accounts",
                    new { status = "reset-request" }
                )
            }, JsonRequestBehavior.AllowGet);
        }
    }
}