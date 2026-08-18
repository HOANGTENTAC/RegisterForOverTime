using HRMS.Common;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.ViewModels;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HRMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly WebCommonModule _userRepo = new WebCommonModule();
        private readonly ApplicationDbContext _context;

        public AccountController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Login
        [HttpGet]
        public ActionResult Login()
        {
            if (Session["LoginInfo"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            var cookie = Request.Cookies["LoginInfo"];
            if (cookie != null)
            {
                string userName = cookie.Values["UserName"];
                string password = cookie.Values["Password"];

                var user = _userRepo.GetUser(userName);
                if (user != null && PasswordHelper.VerifyPassword(password, user.MatKhau))
                {
                    Session["LoginInfo"] = user;
                    return RedirectToAction("Index", "Home");
                }
            }
            return View(new UsersModel());
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UsersModel model)
        {
            var user = _userRepo.GetUser(model.MaNhanVien);
            if (user == null)
            {
                TempData["ToastrMessage"] = "Vui lòng nhập đầy đủ thông tin.";
                TempData["ToastrType"] = "error";
                return RedirectToAction("Login");
            }

            if (!PasswordHelper.VerifyPassword(model.MatKhau, user.MatKhau))
            {
                TempData["ToastrMessage"] = "Mật khẩu không đúng.";
                TempData["ToastrType"] = "error";
                return RedirectToAction("Login");
            }

            if (model.RememberMe)
            {
                HttpCookie cookie = new HttpCookie("LoginInfo");
                cookie.Values["UserName"] = user.MaNhanVien;
                cookie.Values["Password"] = model.MatKhau;
                cookie.Expires = DateTime.Now.AddDays(3);
                Response.Cookies.Add(cookie);
            }

            Session["LoginInfo"] = user;
            return RedirectToAction("Index", "Home");
        }

        // GET: Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            // clear cookie remember me nếu có
            if (Request.Cookies["LoginInfo"] != null)
            {
                var cookie = new HttpCookie("LoginInfo");
                cookie.Expires = DateTime.Now.AddDays(-3); // hết hạn ngay
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string employeeCd = model.EmployeeCD.Trim();

                string sql = @"SELECT MaNhanVien, TenNhanVien
                FROM [MITACOSQL].[dbo].[NHANVIEN]
                WHERE MaNhanVien = @MaNhanVien";

                DataTable dt = SQLHelper.ExecuteDt(sql,
                    new SqlParameter("@MaNhanVien", employeeCd));

                if (dt.Rows.Count == 0)
                {
                    TempData["ToastrType"] = "error";
                    TempData["ToastrMessage"] = "Thông tin mã nhân viên bạn cung cấp không hợp lệ. IT không thể hỗ trợ yêu cầu cấp lại mật khẩu.";

                    return RedirectToAction("ForgotPassword");
                }
                var user = _context.Users.FirstOrDefault(x => x.MaNhanVien == employeeCd);

                if (user == null)
                {
                    user = new UsersModel
                    {
                        MaNhanVien = employeeCd,
                        MatKhau = string.Empty,
                        IsAdmin = false,
                        YeuCauCapLaiMatKhau = true,
                        NgayCapNhat = DateTime.Now
                    };

                    _context.Users.Add(user);
                }
                else
                {
                    user.YeuCauCapLaiMatKhau = true;
                    user.NgayCapNhat = DateTime.Now;
                }

                _context.SaveChanges();

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Yêu cầu cấp lại mật khẩu đã được ghi nhận. Vui lòng chờ IT hỗ trợ.";

                //return RedirectToAction("ForgotPassword");
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = ex.Message;

                return View(model);
            }
        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var loginUser = Session["LoginInfo"] as UsersModel;

                if (loginUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var user = _context.Users.FirstOrDefault(x => x.MaNhanVien == loginUser.MaNhanVien);

                if (user == null)
                {
                    TempData["ToastrType"] = "error";
                    TempData["ToastrMessage"] = "Không tìm thấy tài khoản.";

                    return RedirectToAction("ChangePassword");
                }

                if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.MatKhau))
                {
                    TempData["ToastrType"] = "error";
                    TempData["ToastrMessage"] = "Mật khẩu hiện tại không đúng.";

                    return RedirectToAction("ChangePassword");
                }

                if (model.CurrentPassword == model.NewPassword)
                {
                    TempData["ToastrType"] = "warning";
                    TempData["ToastrMessage"] = "Mật khẩu mới không được trùng với mật khẩu hiện tại.";

                    return RedirectToAction("ChangePassword");
                }

                user.MatKhau = PasswordHelper.HashPassword(model.NewPassword.Trim());
                user.YeuCauCapLaiMatKhau = false;
                user.NgayCapNhat = DateTime.Now;

                _context.SaveChanges();

                TempData["ToastrType"] = "success";
                TempData["ToastrMessage"] = "Đổi mật khẩu thành công.";

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = ex.Message;

                return View(model);
            }
        }
    }
}