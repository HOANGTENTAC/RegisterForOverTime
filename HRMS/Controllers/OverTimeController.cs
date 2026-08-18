using HRMS.Common;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.Services;
using HRMS.ViewModels;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace HRMS.Controllers
{
    public class OverTimeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly PermissionService _permissionService;
        public OverTimeController()
        {
            _db = new ApplicationDbContext();
            _permissionService = new PermissionService();
        }

        // GET: OverTime
        public ActionResult Index()
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var today = DateTime.Today;

            var viewPage = new OverTimePageViewModel
            {
                Employees = LoadEmployees(),
                UserRoles = LoadConfirmUsers(),
                Request = new OverTimeRequestModel
                {
                    DateRequest = today,
                    FromTime = today.AddHours(16).AddMinutes(30),
                    ToTime = today.AddHours(19)
                }
            };

            if (TempData["PendingOverTime"] != null)
            {
                viewPage = (OverTimePageViewModel)TempData["PendingOverTime"];
            }

            return View(viewPage);
        }

        private UsersModel CurrentUser
        {
            get
            {
                return Session["LoginInfo"] as UsersModel;
            }
        }

        private bool CanManageAllOvertime()
        {
            var user = CurrentUser;

            return _permissionService.CanViewAllData(user);
        }

        private bool CanViewOvertimeTicket(int ticketId)
        {
            var user = CurrentUser;

            if (user == null)
            {
                return false;
            }

            if (CanManageAllOvertime())
            {
                return true;
            }

            string sql = @"SELECT TOP 1 t.CreatedUserCD, h.ConfirmUserCD FROM tbl_Tickets t
            INNER JOIN tbl_OvertimeHeaders h ON t.Id = h.TicketId
            WHERE t.Id = @TicketId AND t.TicketTypeId = 1";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@TicketId", ticketId));

            if (dt.Rows.Count == 0)
            {
                return false;
            }

            string createdUser = dt.Rows[0]["CreatedUserCD"].ToString();
            string confirmUser = dt.Rows[0]["ConfirmUserCD"].ToString();

            return string.Equals(createdUser, user.MaNhanVien, StringComparison.OrdinalIgnoreCase)
                || string.Equals(confirmUser, user.MaNhanVien, StringComparison.OrdinalIgnoreCase);
        }

        private List<EmployeeModel> LoadEmployees()
        {
            var currentUser = CurrentUser;

            if (currentUser == null)
            {
                return new List<EmployeeModel>();
            }

            var parameters = new List<SqlParameter>();

            string permissionWhere = PermissionScopeHelper.BuildEmployeeScopeWhere(currentUser: currentUser,
                permissionService: _permissionService, selectedDept: null, employeeAlias: "nv",
                departmentColumnExpression: "nv.MaPhongBan", parameters: parameters);

            if (permissionWhere == "NO_ACCESS")
            {
                return new List<EmployeeModel>();
            }

            string sql = @"
            WITH MonthlyHours AS
            (
                SELECT d.EmployeeCD, SUM(d.HoursWorked) AS MonthlyHours
                FROM tbl_OvertimeHeaders h
                INNER JOIN tbl_OvertimeDetails d ON h.Id = d.OvertimeHeaderId
                INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
                WHERE t.StatusId = 2 
                  AND MONTH(d.OvertimeDate) = MONTH(GETDATE()) 
                  AND YEAR(d.OvertimeDate) = YEAR(GETDATE())
                GROUP BY d.EmployeeCD
            )
            SELECT 
                nv.MaNhanVien, nv.TenNhanVien, nv.MaPhongBan, pb.TenPhongBan, ISNULL(mh.MonthlyHours, 0) AS MonthlyHours
            FROM [MITACOSQL].[dbo].[NHANVIEN] nv
            INNER JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON nv.MaPhongBan = pb.MaPhongBan
            LEFT JOIN MonthlyHours mh ON nv.MaNhanVien = mh.EmployeeCD
            WHERE 1 = 1 ";

            sql += permissionWhere;

            sql += @" ORDER BY pb.TenPhongBan, nv.MaNhanVien";

            DataTable dt = SQLHelper.ExecuteDt(sql, parameters.ToArray());

            var employees = new List<EmployeeModel>();

            foreach (DataRow row in dt.Rows)
            {
                employees.Add(new EmployeeModel
                {
                    MaNhanVien = row["MaNhanVien"].ToString(),
                    TenNhanVien = row["TenNhanVien"].ToString(),
                    MaPhongBan = row["MaPhongBan"].ToString(),
                    TenPhongBan = row["TenPhongBan"].ToString(),
                    MonthlyHours = row["MonthlyHours"] == DBNull.Value ? 0 : Convert.ToDecimal(row["MonthlyHours"]),
                });
            }

            return employees;
        }

        private List<UserRolesModel> LoadConfirmUsers()
        {
            string sql = @"SELECT ur.MaNhanVien, nv.TenNhanVien, ur.BoPhanQuanLy, pb.TenPhongBan AS TenBoPhanQuanLy,
            ur.AccessLevel FROM [TIME_KEEPING].[dbo].[UserRoles] ur
            INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] nv ON ur.MaNhanVien = nv.MaNhanVien
            LEFT JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON ur.BoPhanQuanLy = pb.MaPhongBan
            WHERE ur.BoPhanQuanLy IS NOT NULL AND ur.BoPhanQuanLy <> '' AND ur.AccessLevel BETWEEN 3 AND 4
            ORDER BY nv.TenNhanVien, pb.TenPhongBan;";

            DataTable dt = SQLHelper.ExecuteDt(sql);

            var result = new List<UserRolesModel>();

            foreach (DataRow row in dt.Rows)
            {
                result.Add(new UserRolesModel
                {
                    MaNhanVien = row["MaNhanVien"].ToString(),
                    TenNhanVien = row["TenNhanVien"].ToString(),

                    // Bộ phận mà người này được phân quyền quản lý
                    BoPhanQuanLy = row["BoPhanQuanLy"].ToString(),

                    // Tên bộ phận quản lý
                    TenPhongBan = row["TenBoPhanQuanLy"].ToString(),

                    AccessLevel = Convert.ToInt32(row["AccessLevel"])
                });
            }

            return result;
        }

        [HttpGet]
        public JsonResult CheckDayOff(string date)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(date))
                {
                    return Json(new
                    {
                        success = false,
                        isDayOff = false
                    }, JsonRequestBehavior.AllowGet);
                }

                DateTime ngay;

                if (!DateTime.TryParse(date, out ngay))
                {
                    return Json(new
                    {
                        success = false,
                        isDayOff = false
                    }, JsonRequestBehavior.AllowGet);
                }

                bool isDayOff = CheckDayOffFromDb(ngay);

                return Json(new
                {
                    success = true,
                    isDayOff = isDayOff
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

        private bool CheckDayOffFromDb(DateTime date)
        {
            string column = $"D{date.Day}";
            string sql = $@"SELECT {column}
                    FROM NgayNghi
                    WHERE Nam = @Nam AND Thang = @Thang";
            var result = SQLHelper.ExecuteScalar(sql,
                new SqlParameter("@Nam", date.Year),
                new SqlParameter("@Thang", date.Month));
            return result != null && result.ToString().Equals("True", StringComparison.OrdinalIgnoreCase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(OverTimePageViewModel model)
        {
            var confirmValidation = ValidateConfirmUserForSubmit(model.Request);
            if (!confirmValidation.Success)
            {
                TempData["ToastrMessage"] = confirmValidation.Message;
                TempData["ToastrType"] = "error";

                model.Employees = LoadEmployees();
                model.UserRoles = LoadConfirmUsers();

                return View("Index", model);
            }

            bool isDayOff = CheckDayOffFromDb(model.Request.DateRequest.Date);
            DateTime minDate = DateTime.Today.AddDays(-1);
            if (model.Request.FromTime < minDate || model.Request.ToTime < minDate)
            {
                TempData["ToastrMessage"] = "Thời gian đăng ký tăng ca không được nhỏ hơn 24 giờ trước.";
                TempData["ToastrType"] = "error";
                model.Employees = LoadEmployees();
                model.UserRoles = LoadConfirmUsers();
                return View("Index", model);
            }
            var service = new OvertimeService(new ApplicationDbContext());
            model.Request.OvertimeType = service.CheckTypeOverTime(model.Request.FromTime, model.Request.ToTime, isDayOff);
            model.Request.CreatedUserCD = (Session["LoginInfo"] as UsersModel).MaNhanVien;

            // Nếu loại tăng ca không hợp lệ thì chỉ gán message để View hiển thị confirm
            if (model.Request.OvertimeType < 0 && !model.Request.ForceSubmit)
            {
                ViewBag.ConfirmMessage = "Thời gian đăng ký tăng ca không nằm trong hệ thống. Bạn có muốn tiếp tục?";
                model.Employees = LoadEmployees();
                model.UserRoles = LoadConfirmUsers();
                TempData["PendingOverTime"] = model; // lưu toàn bộ request
                return View("Index", model);
            }

            try
            {
                var result = service.CreateOvertimeRequest(model.Request, isDayOff);
                if (result.Success == true)
                {
                    TempData["ToastrMessage"] = result.Message;
                    TempData["ToastrType"] = "success";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ToastrMessage"] = result.Message;
                    TempData["ToastrType"] = "error";
                    model.Employees = LoadEmployees();
                    model.UserRoles = LoadConfirmUsers();
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = ex.Message;
                TempData["ToastrType"] = "error";
                model.Employees = LoadEmployees();
                model.UserRoles = LoadConfirmUsers();

                TempData["PendingOverTime"] = model; // lưu toàn bộ request
                return View("Index", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(OverTimePageViewModel model)
        {
            if (model.Request.EmployeeCDs == null || !model.Request.EmployeeCDs.Any())
            {
                TempData["ToastrMessage"] = "Bạn phải chọn ít nhất 1 nhân viên đăng ký tăng ca.";
                TempData["ToastrType"] = "error";
                model.Employees = LoadEmployees();
                model.UserRoles = LoadConfirmUsers();
                return View("Edit", model);
            }
            bool isDayOff = CheckDayOffFromDb(model.Request.DateRequest.Date);
            DateTime minDate = DateTime.Today.AddDays(-1);
            if (model.Request.FromTime < minDate || model.Request.ToTime < minDate)
            {
                TempData["ToastrMessage"] = "Thời gian đăng ký tăng ca không được nhỏ hơn 24 giờ trước.";
                TempData["ToastrType"] = "error";
                model.Employees = LoadEmployees();
                model.UserRoles = LoadConfirmUsers();
                return View("Edit", model);
            }
            var service = new OvertimeService(new ApplicationDbContext());
            model.Request.OvertimeType = service.CheckTypeOverTime(model.Request.FromTime, model.Request.ToTime, isDayOff);
            model.Request.CreatedUserCD = (Session["LoginInfo"] as UsersModel).MaNhanVien;
            // Nếu loại tăng ca không hợp lệ thì chỉ gán message để View hiển thị confirm
            if (model.Request.OvertimeType < 0 && !model.Request.ForceSubmit)
            {
                ViewBag.ConfirmMessage = "Thời gian đăng ký tăng ca không nằm trong hệ thống. Bạn có muốn tiếp tục?";
                model.Employees = LoadEmployees();
                model.UserRoles = LoadConfirmUsers();
                TempData["PendingOverTime"] = model;
                return View("Edit", model);
            }
            try
            {
                var result = service.UpdateOvertimeRequest(model.Request, isDayOff);
                if (result.Success == true)
                {
                    TempData["ToastrMessage"] = result.Message;
                    TempData["ToastrType"] = "success";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ToastrMessage"] = result.Message;
                    TempData["ToastrType"] = "error";
                    model.Employees = LoadEmployees();
                    model.UserRoles = LoadConfirmUsers();
                    return View("Edit", model);
                }
            }
            catch (Exception ex)
            {
                TempData["ToastrMessage"] = ex.Message;
                TempData["ToastrType"] = "error";
                model.Employees = LoadEmployees();
                model.UserRoles = LoadConfirmUsers();
                TempData["PendingOverTime"] = model;
                return View("Edit", model);
            }
        }
        public ActionResult Detail(int tblTicketId = 0)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (tblTicketId == 0)
            {
                return RedirectToAction("Index");
            }

            if (!CanViewOvertimeTicket(tblTicketId))
            {
                TempData["ToastrMessage"] = "Bạn không có quyền xem phiếu tăng ca này.";
                TempData["ToastrType"] = "error";

                return RedirectToAction("Index", "Home");
            }
            ViewBag.CanManageAllOvertime = CanManageAllOvertime();
            string mySQL = string.Empty;
            mySQL = @"WITH MonthlyHours AS (
                        SELECT 
                            d.EmployeeCD,
                            SUM(d.HoursWorked) AS MonthlyHours
                        FROM tbl_OvertimeHeaders h
                        INNER JOIN tbl_OvertimeDetails d ON h.Id = d.OvertimeHeaderId
                        INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
                        WHERE t.StatusId = 2
                          AND MONTH(d.OvertimeDate) = MONTH(GETDATE())
                          AND YEAR(d.OvertimeDate) = YEAR(GETDATE())
                        GROUP BY d.EmployeeCD
                    )
                    SELECT 
                        t.TicketNo, s.StatusName, t.CreatedUserCD, h.RequestDate, h.OvertimeType, h.ConfirmUserCD, c.TenNhanVien AS ConfirmUserName,
                        h.FromTime, h.ToTime, h.Reason as ReasonRequest, d.EmployeeCD, e.TenNhanVien AS EmployeeName, d.OvertimeDate, t.StatusId, h.Id,
                        d.HoursWorked, mh.MonthlyHours, t.Reason
                    FROM tbl_OvertimeHeaders h
                    INNER JOIN tbl_OvertimeDetails d ON h.Id = d.OvertimeHeaderId 
                    INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
                    INNER JOIN mst_TicketStatus s ON t.StatusId = s.StatusId
                    INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] e ON d.EmployeeCD = e.MaNhanVien 
                    INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] c ON h.ConfirmUserCD = c.MaNhanVien 
                    LEFT JOIN MonthlyHours mh ON d.EmployeeCD = mh.EmployeeCD
                    WHERE t.Id = @tblTicketId AND TicketTypeId = 1 ";

            DataTable dt = SQLHelper.ExecuteDt(mySQL, new SqlParameter("@tblTicketId", tblTicketId));
            if (dt.Rows.Count == 0)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu tăng ca.";
                return RedirectToAction("Index");
            }

            var ticket = new TblTicketsModel
            {
                Id = tblTicketId,
                TicketNo = dt.Rows[0]["TicketNo"].ToString(),
                StatusId = Convert.ToInt32(dt.Rows[0]["StatusId"]),
                StatusName = dt.Rows[0]["StatusName"].ToString(),
                CreatedUserCD = dt.Rows[0]["CreatedUserCD"].ToString(),
                Reason = dt.Rows[0]["Reason"].ToString()
            };

            var header = new TblOvertimeHeadersModel
            {
                Id = Convert.ToInt32(dt.Rows[0]["Id"]),
                TicketId = tblTicketId,
                RequestDate = Convert.ToDateTime(dt.Rows[0]["RequestDate"]),
                OvertimeType = Convert.ToInt32(dt.Rows[0]["OvertimeType"]),
                ConfirmUserCD = dt.Rows[0]["ConfirmUserCD"].ToString(),
                ConfirmUserName = dt.Rows[0]["ConfirmUserName"].ToString(),
                FromTime = Convert.ToDateTime(dt.Rows[0]["FromTime"]),
                ToTime = Convert.ToDateTime(dt.Rows[0]["ToTime"]),
                Reason = dt.Rows[0]["ReasonRequest"].ToString()
            };

            var details = dt.AsEnumerable()
                .Select(row => new TblOvertimeDetailsModel
                {
                    OvertimeHeaderId = header.Id,
                    EmployeeCD = row["EmployeeCD"].ToString(),
                    EmployeeName = row["EmployeeName"].ToString(),
                    OvertimeDate = Convert.ToDateTime(row["OvertimeDate"]),
                    HoursWorked = Convert.ToDecimal(row["HoursWorked"]),
                    MonthlyHours = row["MonthlyHours"] != DBNull.Value ? Convert.ToDecimal(row["MonthlyHours"]) : 0
                }).ToList();

            var model = new OverTimeDetailPageViewModel
            {
                Ticket = ticket,
                Header = header,
                Details = details
            };

            return View(model);
        }

        public ActionResult Edit(int tblTicketId = 0)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (tblTicketId == 0)
            {
                return RedirectToAction("Index");
            }

            if (!CanViewOvertimeTicket(tblTicketId))
            {
                TempData["ToastrMessage"] = "Bạn không có quyền chỉnh sửa phiếu này.";
                TempData["ToastrType"] = "error";

                return RedirectToAction("Index", "Home");
            }

            string mySQL = @"SELECT t.Id, t.TicketNo, t.StatusId, h.RequestDate, h.OvertimeType, h.FromTime, h.ToTime, h.ConfirmUserCD,
                h.Reason, d.EmployeeCD, e.TenNhanVien AS EmployeeName, d.OvertimeDate, d.HoursWorked
                FROM tbl_OvertimeHeaders h
                INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
                INNER JOIN tbl_OvertimeDetails d ON h.Id = d.OvertimeHeaderId
                INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] e ON d.EmployeeCD = e.MaNhanVien
                WHERE t.Id = @tblTicketId 
                  AND t.TicketTypeId = 1 ";

            DataTable dt = SQLHelper.ExecuteDt(mySQL, new SqlParameter("@tblTicketId", tblTicketId));
            if (dt.Rows.Count == 0)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu tăng ca.";
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index");
            }

            int statusId = Convert.ToInt32(dt.Rows[0]["StatusId"]);

            if (statusId != 1)
            {
                TempData["ToastrMessage"] = "Phiếu đã được xử lý, bạn không thể chỉnh sửa.";
                TempData["ToastrType"] = "error";

                return RedirectToAction("Detail", new { tblTicketId });
            }

            var viewModel = new OverTimePageViewModel
            {
                Request = new OverTimeRequestModel
                {
                    TicketId = Convert.ToInt32(dt.Rows[0]["Id"]),
                    DateRequest = Convert.ToDateTime(dt.Rows[0]["RequestDate"].ToString()),
                    OvertimeType = Convert.ToInt32(dt.Rows[0]["OvertimeType"]),
                    FromTime = Convert.ToDateTime(dt.Rows[0]["FromTime"]),
                    ToTime = Convert.ToDateTime(dt.Rows[0]["ToTime"]),
                    ConfirmUserCD = dt.Rows[0]["ConfirmUserCD"].ToString(),
                    Reason = dt.Rows[0]["Reason"].ToString(),
                    EmployeeCDs = dt.AsEnumerable().Select(row => new EmployeeModel
                    {
                        MaNhanVien = row["EmployeeCD"].ToString()
                    }).ToList()

                },
                Employees = LoadEmployees(),
                UserRoles = LoadConfirmUsers()
            };

            return View(viewModel);
        }

        public ActionResult Accept(int tblTicketId)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = CurrentUser;

            bool canForceApprove = CanManageAllOvertime();

            var service = new OvertimeService(new ApplicationDbContext());

            var result = service.ApproveOvertimeRequest(tblTicketId, currentUser.MaNhanVien, true, null, canForceApprove);

            if (result.Success)
            {
                TempData["ToastrMessage"] = result.Message;
                TempData["ToastrType"] = "success";
                return RedirectToAction("Index", "Home");
            }

            TempData["ToastrMessage"] = result.Message;
            TempData["ToastrType"] = "error";
            return RedirectToAction("Detail", new { tblTicketId });
        }

        public ActionResult Reject(int tblTicketId, string reason)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = CurrentUser;

            bool canForceApprove = CanManageAllOvertime();

            var service = new OvertimeService(new ApplicationDbContext());

            var result = service.ApproveOvertimeRequest(tblTicketId, currentUser.MaNhanVien, false, reason, canForceApprove);

            if (result.Success)
            {
                TempData["ToastrMessage"] = result.Message;
                TempData["ToastrType"] = "success";
                return RedirectToAction("Index", "Home");
            }

            TempData["ToastrMessage"] = result.Message;
            TempData["ToastrType"] = "error";
            return RedirectToAction("Detail", new { tblTicketId });
        }

        public ActionResult Delete(int tblTicketId)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = CurrentUser;

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            bool canForceDelete = CanManageAllOvertime();

            var service = new OvertimeService(new ApplicationDbContext());

            var result = service.DeleteOvertimeRequest(tblTicketId, currentUser.MaNhanVien, canForceDelete);

            if (result.Success)
            {
                TempData["ToastrMessage"] = result.Message;
                TempData["ToastrType"] = "success";
                return RedirectToAction("Index", "Home");
            }

            TempData["ToastrMessage"] = result.Message;
            TempData["ToastrType"] = "error";
            return RedirectToAction("Detail", new { tblTicketId });
        }

        [HttpPost]
        public JsonResult RemoveDetail(int ticketID, string employeeCD, int headerID)
        {
            if (Session["LoginInfo"] == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Phiên đăng nhập đã hết hạn."
                });
            }

            var currentUser = CurrentUser;

            bool canForceModify = CanManageAllOvertime();

            var ticket = _db.TblTickets.FirstOrDefault(t => t.Id == ticketID);

            if (ticket == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy phiếu."
                });
            }

            var header = _db.TblOvertimeHeaders.FirstOrDefault(h => h.Id == headerID);

            if (header == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin tăng ca."
                });
            }

            bool isCreator = ticket.CreatedUserCD == currentUser.MaNhanVien;
            bool isConfirmUser = header.ConfirmUserCD == currentUser.MaNhanVien;
            bool isPending = ticket.StatusId == (int)Enums.RequestStatusEnum.Pending;

            if (!canForceModify)
            {
                if (!isPending)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiếu đã được xử lý, bạn không thể xoá nhân viên trong phiếu."
                    });
                }

                if (!isCreator && !isConfirmUser)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bạn không có quyền xoá nhân viên trong phiếu này."
                    });
                }
            }

            var detail = _db.TblOvertimeDetails
                .FirstOrDefault(d => d.EmployeeCD == employeeCD && d.OvertimeHeaderId == headerID);

            if (detail == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy nhân viên trong phiếu."
                });
            }

            _db.TblOvertimeDetails.Remove(detail);
            _db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Đã xoá nhân viên khỏi phiếu."
            });
        }

        public ActionResult Export(int? tblTicketId)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (tblTicketId == null)
            {
                TempData["ToastrMessage"] = "Không có dữ liệu tăng ca";
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index", "Home");
            }

            if (!CanViewOvertimeTicket(tblTicketId.Value))
            {
                TempData["ToastrMessage"] = "Bạn không có quyền export phiếu này.";
                TempData["ToastrType"] = "error";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                string mySQL = $@"
                WITH MonthlyHours AS (
                    SELECT 
                        d.EmployeeCD,
                        SUM(d.HoursWorked) AS MonthlyHours
                    FROM tbl_OvertimeHeaders h
                    INNER JOIN tbl_OvertimeDetails d ON h.Id = d.OvertimeHeaderId
                    INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
                    WHERE t.StatusId = 2
                      AND MONTH(d.OvertimeDate) = MONTH(GETDATE())
                      AND YEAR(d.OvertimeDate) = YEAR(GETDATE())
                    GROUP BY d.EmployeeCD
                )
                SELECT 
                    t.TicketNo, h.RequestDate, d.OvertimeDate, h.FromTime, h.ToTime, h.Reason as ReasonRequest, 
                	d.EmployeeCD, e.TenNhanVien AS EmployeeName, p.TenPhongBan, d.HoursWorked, mh.MonthlyHours
                FROM tbl_OvertimeHeaders h
                INNER JOIN tbl_OvertimeDetails d ON h.Id = d.OvertimeHeaderId 
                INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
                INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] e ON d.EmployeeCD = e.MaNhanVien 
                INNER JOIN [MITACOSQL].[dbo].[PHONGBAN] p on e.MaPhongBan = p.MaPhongBan
                LEFT JOIN MonthlyHours mh ON d.EmployeeCD = mh.EmployeeCD
                WHERE t.Id = @tblTicketId AND TicketTypeId = 1";
                DataTable dt = SQLHelper.ExecuteDt(mySQL, new SqlParameter("@tblTicketId", tblTicketId));
                if (dt.Rows.Count == 0)
                {
                    TempData["ToastrMessage"] = "Không tìm thấy dữ liệu phiếu";
                    TempData["ToastrType"] = "error";
                    return RedirectToAction("Detail", new { tblTicketId });
                }

                //string templatePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Resources", "RegisterOvertime.xlsx");
                string templatePath = Server.MapPath("~/Resources/RegisterOvertime.xlsx");
                var groups = dt.AsEnumerable().GroupBy(r => r.Field<string>("TenPhongBan"));
                string ticketNo = dt.Rows[0]["TicketNo"].ToString();
                string requestDate = Convert.ToDateTime(dt.Rows[0]["RequestDate"]).ToString("yyyy/MM/dd");
                string overtimeDate = Convert.ToDateTime(dt.Rows[0]["OvertimeDate"]).ToString("yyyy/MM/dd");
                string reasonRequest = dt.Rows[0]["ReasonRequest"].ToString();

                if (groups.Count() == 1)
                {
                    var group = groups.First();
                    using (FileStream fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                    {
                        var workbook = new XSSFWorkbook(fs);
                        var sheet = workbook.GetSheet("sheet2");
                        var firstRow = group.First();
                        int sheetIndex = workbook.GetSheetIndex(sheet);
                        workbook.SetSheetName(sheetIndex, Convert.ToDateTime(firstRow["OvertimeDate"]).ToString("MM.dd"));

                        sheet.GetRow(4).GetCell(2).SetCellValue(requestDate);
                        sheet.GetRow(4).GetCell(5).SetCellValue(overtimeDate);
                        sheet.GetRow(4).GetCell(8).SetCellValue(group.Key);
                        sheet.GetRow(7).GetCell(2).SetCellValue(reasonRequest);

                        int startRow = 11;
                        int i = 0;
                        foreach (var dr in group)
                        {
                            int currentRow = startRow + i;
                            var templateRow = sheet.GetRow(startRow);
                            var newRow = sheet.GetRow(currentRow) ?? sheet.CreateRow(currentRow);

                            for (int c = 0; c < templateRow.LastCellNum; c++)
                            {
                                var templateCell = templateRow.GetCell(c);
                                var newCell = newRow.GetCell(c) ?? newRow.CreateCell(c);
                                if (templateCell != null)
                                {
                                    newCell.CellStyle = templateCell.CellStyle;
                                }
                            }

                            newRow.GetCell(0).SetCellValue(i + 1);
                            newRow.GetCell(1).SetCellValue(dr["EmployeeCD"].ToString());
                            newRow.GetCell(2).SetCellValue(dr["EmployeeName"].ToString());
                            newRow.GetCell(3).SetCellValue(Convert.ToDateTime(dr["FromTime"]).ToString("HH:mm"));
                            newRow.GetCell(4).SetCellValue(Convert.ToDateTime(dr["ToTime"]).ToString("HH:mm"));
                            newRow.GetCell(7).SetCellValue(Convert.ToDouble(dr["HoursWorked"]));
                            if (!string.IsNullOrEmpty(dr["MonthlyHours"].ToString()))
                            {
                                newRow.GetCell(8).SetCellValue(Convert.ToDouble(dr["MonthlyHours"]));
                            }

                            i++;
                        }

                        using (var ms = new MemoryStream())
                        {
                            workbook.Write(ms);

                            string fileName = $"{ticketNo}_{group.Key}_{DateTime.Now:yyyyMMdd}.xlsx";
                            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
                }
                else
                {
                    using (var zipStream = new MemoryStream())
                    {
                        using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
                        {
                            foreach (var group in groups)
                            {
                                using (FileStream fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                                {
                                    var workbook = new XSSFWorkbook(fs);
                                    var sheet = workbook.GetSheet("sheet2");

                                    var firstRow = group.First();
                                    int sheetIndex = workbook.GetSheetIndex(sheet);
                                    workbook.SetSheetName(sheetIndex, Convert.ToDateTime(firstRow["OvertimeDate"]).ToString("MM.dd"));

                                    sheet.GetRow(4).GetCell(2).SetCellValue(requestDate);
                                    sheet.GetRow(4).GetCell(5).SetCellValue(overtimeDate);
                                    sheet.GetRow(4).GetCell(8).SetCellValue(group.Key);
                                    sheet.GetRow(7).GetCell(2).SetCellValue(reasonRequest);

                                    int startRow = 11;
                                    int i = 0;
                                    foreach (var dr in group)
                                    {
                                        int currentRow = startRow + i;
                                        var templateRow = sheet.GetRow(startRow);
                                        var newRow = sheet.GetRow(currentRow) ?? sheet.CreateRow(currentRow);

                                        for (int c = 0; c < templateRow.LastCellNum; c++)
                                        {
                                            var templateCell = templateRow.GetCell(c);
                                            var newCell = newRow.GetCell(c) ?? newRow.CreateCell(c);
                                            if (templateCell != null)
                                                newCell.CellStyle = templateCell.CellStyle;
                                        }

                                        newRow.GetCell(0).SetCellValue(i + 1);
                                        newRow.GetCell(1).SetCellValue(dr["EmployeeCD"].ToString());
                                        newRow.GetCell(2).SetCellValue(dr["EmployeeName"].ToString());
                                        newRow.GetCell(3).SetCellValue(Convert.ToDateTime(dr["FromTime"]).ToString("HH:mm"));
                                        newRow.GetCell(4).SetCellValue(Convert.ToDateTime(dr["ToTime"]).ToString("HH:mm"));
                                        newRow.GetCell(7).SetCellValue(Convert.ToDouble(dr["HoursWorked"]));
                                        if (!string.IsNullOrEmpty(dr["MonthlyHours"].ToString()))
                                        {
                                            newRow.GetCell(8).SetCellValue(Convert.ToDouble(dr["MonthlyHours"]));
                                        }
                                        i++;
                                    }

                                    using (var ms = new MemoryStream())
                                    {
                                        workbook.Write(ms);
                                        byte[] fileBytes = ms.ToArray();

                                        string safeDeptName = string.Join("_", group.Key.Split(Path.GetInvalidFileNameChars()));
                                        string fileName = $"{ticketNo}_{safeDeptName}_{DateTime.Now:yyyyMMdd}.xlsx";

                                        var entry = archive.CreateEntry(fileName);
                                        using (var entryStream = entry.Open())
                                        {
                                            entryStream.Write(fileBytes, 0, fileBytes.Length);
                                        }
                                    }
                                }
                            }
                        }

                        return File(zipStream.ToArray(), "application/zip", $"{ticketNo}_{DateTime.Now:yyyyMMdd}.zip");
                    }
                }
            }
        }

        private ServiceResult ValidateConfirmUserForSubmit(OverTimeRequestModel request)
        {
            var currentUser = CurrentUser;

            if (currentUser == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Phiên đăng nhập đã hết hạn."
                };
            }

            if (request == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Dữ liệu đăng ký không hợp lệ."
                };
            }

            if (request.EmployeeCDs == null || !request.EmployeeCDs.Any())
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Bạn phải chọn ít nhất 1 nhân viên đăng ký tăng ca."
                };
            }

            var employeeCodes = request.EmployeeCDs
                .Where(x => !string.IsNullOrEmpty(x.MaNhanVien))
                .Select(x => x.MaNhanVien)
                .Distinct()
                .ToList();

            if (employeeCodes.Count == 0)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Bạn phải chọn ít nhất 1 nhân viên đăng ký tăng ca."
                };
            }

            var employeeDepartments = LoadEmployeeDepartments(employeeCodes);

            if (employeeDepartments.Count == 0)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Không xác định được bộ phận của nhân viên đã chọn."
                };
            }

            /*
               Nếu người viết phiếu có quyền quản lý toàn bộ bộ phận
               của các nhân viên đã chọn thì tự duyệt luôn.
            */
            if (CanUserConfirmDepartments(currentUser.MaNhanVien, employeeDepartments))
            {
                request.ConfirmUserCD = currentUser.MaNhanVien;
                request.AutoApprove = true;

                return new ServiceResult
                {
                    Success = true,
                    Message = ""
                };
            }

            /*
               Nếu người viết không có quyền tự duyệt,
               bắt buộc phải chọn người xác nhận.
            */
            if (string.IsNullOrEmpty(request.ConfirmUserCD))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Vui lòng chọn người xác nhận phù hợp."
                };
            }

            /*
               Người xác nhận phải quản lý được toàn bộ bộ phận
               của nhân viên được chọn.
            */
            if (!CanUserConfirmDepartments(request.ConfirmUserCD, employeeDepartments))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Người xác nhận không có quyền quản lý tất cả bộ phận của nhân viên đã chọn."
                };
            }

            request.AutoApprove = false;

            return new ServiceResult
            {
                Success = true,
                Message = ""
            };
        }

        private List<string> LoadEmployeeDepartments(List<string> employeeCodes)
        {
            if (employeeCodes == null || employeeCodes.Count == 0)
            {
                return new List<string>();
            }

            var parameters = new List<SqlParameter>();
            var parameterNames = new List<string>();

            for (int i = 0; i < employeeCodes.Count; i++)
            {
                string paramName = "@Emp" + i;

                parameterNames.Add(paramName);
                parameters.Add(new SqlParameter(paramName, employeeCodes[i]));
            }

            string sql = $@"SELECT DISTINCT MaPhongBan
            FROM [MITACOSQL].[dbo].[NHANVIEN]
            WHERE MaNhanVien IN ({string.Join(",", parameterNames)})
            AND MaPhongBan IS NOT NULL
            AND MaPhongBan <> ''";

            DataTable dt = SQLHelper.ExecuteDt(sql, parameters.ToArray());

            return dt.AsEnumerable().Select(x => x["MaPhongBan"].ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();
        }

        private bool CanUserConfirmDepartments(string userCd, List<string> departmentCodes)
        {
            if (string.IsNullOrEmpty(userCd) || departmentCodes == null || departmentCodes.Count == 0)
            {
                return false;
            }

            string sql = @"SELECT DISTINCT BoPhanQuanLy FROM [TIME_KEEPING].[dbo].[UserRoles]
            WHERE MaNhanVien = @MaNhanVien AND AccessLevel BETWEEN 3 AND 4
            AND BoPhanQuanLy IS NOT NULL AND BoPhanQuanLy <> ''";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@MaNhanVien", userCd));

            var userDepartments = dt.AsEnumerable()
                .Select(x => x["BoPhanQuanLy"].ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            if (userDepartments.Count == 0)
            {
                return false;
            }

            return departmentCodes.All(dept =>
                userDepartments.Any(x =>
                    string.Equals(x, dept, StringComparison.OrdinalIgnoreCase)));
        }

    }
}