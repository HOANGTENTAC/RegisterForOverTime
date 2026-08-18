using HRMS.Filters;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.Services;
using HRMS.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HRMS.Controllers
{
    [AdminAuthorize]
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AccountsService _accountsService;

        public AccountsController()
        {
            _context = new ApplicationDbContext();
            _accountsService = new AccountsService(_context);
        }

        public ActionResult Index(string keyword, string dept, int? accessLevel, string status)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var users = _accountsService.LoadUsers(keyword, dept, accessLevel, status);

            var model = new AccountPageViewModel
            {
                Keyword = keyword,
                SelectedDept = dept,
                SelectedAccessLevel = accessLevel,
                SelectedStatus = status,

                Departments = _context.Departments.ToList(),
                Users = users,

                TotalAccounts = users.Count(x => x.HasAccount),
                TotalAdmins = users.Count(x => AccessLevelHelper.IsAdminLevel(x.HighestAccessLevel)),
                TotalManagers = users.Count(x => AccessLevelHelper.IsManagerLevel(x.HighestAccessLevel)),
                TotalNoAccount = users.Count(x => !x.HasAccount)
            };

            ViewBag.AccessLevels = AccessLevelHelper.GetSelectList(accessLevel);

            return View(model);
        }

        [HttpGet]
        public JsonResult Data(string keyword, string dept, int? accessLevel, string status)
        {
            try
            {
                if (Session["LoginInfo"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn."
                    }, JsonRequestBehavior.AllowGet);
                }

                var users = _accountsService.LoadUsers(keyword, dept, accessLevel, status);

                return Json(new
                {
                    success = true,

                    summary = new
                    {
                        totalAccounts = users.Count(x => x.HasAccount),
                        totalAdmins = users.Count(x => AccessLevelHelper.IsAdminLevel(x.HighestAccessLevel)),
                        totalManagers = users.Count(x => AccessLevelHelper.IsManagerLevel(x.HighestAccessLevel)),
                        totalNoAccount = users.Count(x => !x.HasAccount)
                    },

                    rows = users.Select(x => new
                    {
                        x.EmployeeCD,
                        x.TenNhanVien,
                        x.MaPhongBan,
                        x.TenPhongBan,
                        x.HasAccount,
                        x.HighestAccessLevel,
                        x.HighestAccessLevelName,
                        x.ManagedDepartmentsText,
                        x.ManagedDepartmentsCount,
                        x.YeuCauCapLaiMatKhau,
                        NgayCapNhat = x.NgayCapNhat.HasValue
                            ? x.NgayCapNhat.Value.ToString("dd/MM/yyyy HH:mm")
                            : "",
                        x.TrangThai
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetRoles(string employeeCd)
        {
            try
            {
                if (Session["LoginInfo"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn."
                    }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrEmpty(employeeCd))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Thiếu mã nhân viên."
                    }, JsonRequestBehavior.AllowGet);
                }

                var roles = _accountsService.LoadRoles(employeeCd);

                return Json(new
                {
                    success = true,

                    rows = roles.Select(x => new
                    {
                        x.EmployeeCD,
                        x.TenNhanVien,
                        x.BoPhanQuanLy,
                        x.TenBoPhanQuanLy,
                        x.AccessLevel,
                        x.AccessLevelName,

                        NgayCapNhat = x.NgayCapNhat.HasValue
                            ? x.NgayCapNhat.Value.ToString("dd/MM/yyyy HH:mm")
                            : ""
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveRole(SaveAccountRoleRequest request)
        {
            try
            {
                if (Session["LoginInfo"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn."
                    });
                }

                var result = _accountsService.SaveRole(request);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult CreateAccount(string employeeCd, string password)
        {
            try
            {
                if (Session["LoginInfo"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn."
                    });
                }

                var result = _accountsService.CreateAccount(employeeCd, password);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult DeleteRole(string employeeCd, string dept)
        {
            try
            {
                if (Session["LoginInfo"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn."
                    });
                }

                var result = _accountsService.DeleteRole(employeeCd, dept);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult ResetPassword(ResetPasswordRequest request)
        {
            try
            {
                if (Session["LoginInfo"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn."
                    });
                }

                var result = _accountsService.ResetPassword(request);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}