using HRMS.Common;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.Services;
using HRMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace HRMS.Controllers
{
    public class ShiftRegisterController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly PermissionService _permissionService;

        public ShiftRegisterController()
        {
            _db = new ApplicationDbContext();
            _permissionService = new PermissionService();
        }

        private UsersModel CurrentUser
        {
            get
            {
                return Session["LoginInfo"] as UsersModel;
            }
        }

        public ActionResult Index()
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var today = DateTime.Today;

            var employees = LoadEmployees();

            if (employees == null || employees.Count == 0)
            {
                Response.StatusCode = 404;

                ViewBag.Title = "Không áp dụng đăng ký ca";
                ViewBag.ErrorTitle = "Chức năng đăng ký ca không áp dụng";
                ViewBag.ErrorMessage = "Bộ phận của bạn đang sử dụng ca mặc định. Nếu cần thay đổi ca làm việc, vui lòng sử dụng chức năng Đổi ca.";
                ViewBag.BackUrl = Url.Action("Index", "Home");
                ViewBag.BackText = "Quay về trang chủ";

                return View("~/Views/Shared/NotFound.cshtml");
            }

            var model = new ShiftRegisterPageViewModel
            {
                Employees = LoadEmployees(),
                UserRoles = LoadConfirmUsers(),
                ShiftTypes = LoadShiftTypes(),
                Request = new ShiftRegisterRequestModel
                {
                    FromDate = today,
                    ToDate = today
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ShiftRegisterPageViewModel model)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (model.Request.EmployeeCDs == null ||
                !model.Request.EmployeeCDs.Any(x => !string.IsNullOrEmpty(x.MaNhanVien)))
            {
                TempData["ToastrMessage"] = "Bạn phải chọn ít nhất 1 nhân viên đăng ký ca.";
                TempData["ToastrType"] = "error";

                ReloadPageData(model);

                return View("Index", model);
            }

            var employeeCodes = model.Request.EmployeeCDs.Where(x => !string.IsNullOrEmpty(x.MaNhanVien))
                .Select(x => x.MaNhanVien)
                .Distinct()
                .ToList();

            var departmentValidation = ValidateDepartmentsCanUseShiftRegister(employeeCodes, model.Request.FromDate, model.Request.ToDate);

            if (!departmentValidation.Success)
            {
                TempData["ToastrMessage"] = departmentValidation.Message;
                TempData["ToastrType"] = "error";

                ReloadPageData(model);

                return View("Index", model);
            }

            var statusResult = PrepareStatusForSubmit(model.Request);

            if (!statusResult.Success)
            {
                TempData["ToastrMessage"] = statusResult.Message;
                TempData["ToastrType"] = "error";

                ReloadPageData(model);

                return View("Index", model);
            }

            model.Request.CreatedUserCD = CurrentUser.MaNhanVien;

            var service = new ShiftRegisterService(new ApplicationDbContext());

            var result = service.CreateShiftRegisterRequest(model.Request);

            if (result.Success)
            {
                TempData["ToastrMessage"] = result.Message;
                TempData["ToastrType"] = "success";

                return RedirectToAction("Index", "Home");
            }

            TempData["ToastrMessage"] = result.Message;
            TempData["ToastrType"] = "error";

            ReloadPageData(model);

            return View("Index", model);
        }

        private void ReloadPageData(ShiftRegisterPageViewModel model)
        {
            model.Employees = LoadEmployees();
            model.UserRoles = LoadConfirmUsers();
            model.ShiftTypes = LoadShiftTypes();
        }

        private List<MstShiftTypesModel> LoadShiftTypes()
        {
            return _db.MstShiftTypes
                .OrderBy(x => x.ShiftTypeId)
                .ToList();
        }

        private List<EmployeeModel> LoadEmployees()
        {
            var currentUser = CurrentUser;

            if (currentUser == null)
            {
                return new List<EmployeeModel>();
            }

            var parameters = new List<SqlParameter>();

            string permissionWhere = PermissionScopeHelper.BuildEmployeeScopeWhere(
                currentUser: currentUser, permissionService: _permissionService, selectedDept: null,
                employeeAlias: "nv", departmentColumnExpression: "nv.MaPhongBan", parameters: parameters);

            if (permissionWhere == "NO_ACCESS")
            {
                return new List<EmployeeModel>();
            }
            string sql = @"SELECT nv.MaNhanVien, nv.TenNhanVien, nv.MaPhongBan, pb.TenPhongBan
            FROM [MITACOSQL].[dbo].[NHANVIEN] nv
            INNER JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON nv.MaPhongBan = pb.MaPhongBan
            WHERE 1 = 1
              AND NOT EXISTS
              (
                  SELECT 1
                  FROM [TIME_KEEPING].[dbo].[mst_DefaultShifts] ds
                  WHERE ds.DepartmentCD = nv.MaPhongBan
                    AND ds.IsActive = 1
                    AND ds.EffectiveFrom <= CAST(GETDATE() AS date)
                    AND (
                            ds.EffectiveTo IS NULL
                            OR ds.EffectiveTo >= CAST(GETDATE() AS date)
                        )
              )";

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
                    TenPhongBan = row["TenPhongBan"].ToString()
                });
            }

            return employees;
        }

        private ServiceResult ValidateDepartmentsCanUseShiftRegister(List<string> employeeCodes, DateTime fromDate, DateTime toDate)
        {
            if (employeeCodes == null || employeeCodes.Count == 0)
            {
                return Fail("Bạn phải chọn ít nhất 1 nhân viên đăng ký ca.");
            }

            var parameters = new List<SqlParameter>();
            var names = new List<string>();

            for (int i = 0; i < employeeCodes.Count; i++)
            {
                string name = "@Emp" + i;
                names.Add(name);
                parameters.Add(new SqlParameter(name, employeeCodes[i]));
            }

            parameters.Add(new SqlParameter("@FromDate", fromDate.Date));
            parameters.Add(new SqlParameter("@ToDate", toDate.Date));

            string sql = $@"SELECT DISTINCT nv.MaPhongBan, pb.TenPhongBan FROM [MITACOSQL].[dbo].[NHANVIEN] nv
            INNER JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON nv.MaPhongBan = pb.MaPhongBan
            INNER JOIN [TIME_KEEPING].[dbo].[mst_DefaultShifts] ds ON nv.MaPhongBan = ds.DepartmentCD
            WHERE nv.MaNhanVien IN ({string.Join(",", names)})
              AND ds.IsActive = 1
              AND ds.EffectiveFrom <= @ToDate
              AND (
                    ds.EffectiveTo IS NULL
                    OR ds.EffectiveTo >= @FromDate
                  )";

            DataTable dt = SQLHelper.ExecuteDt(sql, parameters.ToArray());

            if (dt.Rows.Count == 0)
            {
                return Ok("");
            }

            var departmentNames = dt.AsEnumerable()
                .Select(x => x["TenPhongBan"].ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            return Fail(
                "Các bộ phận sau đang dùng ca mặc định, không được đăng ký ca tại màn này: "
                + string.Join(", ", departmentNames)
                + ". Vui lòng dùng chức năng Đổi ca nếu cần thay đổi ca làm việc."
            );
        }

        private List<UserRolesModel> LoadConfirmUsers()
        {
            string sql = @"SELECT ur.MaNhanVien, nv.TenNhanVien, ur.BoPhanQuanLy, pb.TenPhongBan AS TenBoPhanQuanLy, ur.AccessLevel
                FROM [TIME_KEEPING].[dbo].[UserRoles] ur
                INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] nv ON ur.MaNhanVien = nv.MaNhanVien
                LEFT JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON ur.BoPhanQuanLy = pb.MaPhongBan
                WHERE ur.BoPhanQuanLy IS NOT NULL AND ur.BoPhanQuanLy <> '' AND ur.AccessLevel BETWEEN 3 AND 4
                ORDER BY nv.TenNhanVien, pb.TenPhongBan";

            DataTable dt = SQLHelper.ExecuteDt(sql);

            var result = new List<UserRolesModel>();

            foreach (DataRow row in dt.Rows)
            {
                result.Add(new UserRolesModel
                {
                    MaNhanVien = row["MaNhanVien"].ToString(),
                    TenNhanVien = row["TenNhanVien"].ToString(),
                    BoPhanQuanLy = row["BoPhanQuanLy"].ToString(),
                    TenPhongBan = row["TenBoPhanQuanLy"].ToString(),
                    AccessLevel = Convert.ToInt32(row["AccessLevel"])
                });
            }

            return result;
        }

        private ServiceResult PrepareStatusForSubmit(ShiftRegisterRequestModel request)
        {
            var currentUser = CurrentUser;

            if (currentUser == null)
            {
                return Fail("Phiên đăng nhập đã hết hạn.");
            }

            var employeeCodes = request.EmployeeCDs
                .Where(x => !string.IsNullOrEmpty(x.MaNhanVien))
                .Select(x => x.MaNhanVien)
                .Distinct()
                .ToList();

            var departments = LoadEmployeeDepartments(employeeCodes);

            if (departments.Count == 0)
            {
                return Fail("Không xác định được bộ phận của nhân viên đã chọn.");
            }

            /*
               Admin / AccessLevel 5:
               Nạp thẳng dữ liệu, không cần gửi duyệt.
            */
            if (CanAdminShiftRegister())
            {
                request.ConfirmUserCD = currentUser.MaNhanVien;
                request.CreateAsFinished = true;
                request.CreateAsManagerAccepted = false;

                return Ok("");
            }

            /*
               Quản lý viết phiếu cho đúng bộ phận mình quản lý:
               Đi thẳng qua bước quản lý duyệt.
               Sau đó chờ HR hoàn tất.
            */
            if (CanUserConfirmDepartments(currentUser.MaNhanVien, departments))
            {
                request.ConfirmUserCD = currentUser.MaNhanVien;
                request.CreateAsManagerAccepted = true;
                request.CreateAsFinished = false;

                return Ok("");
            }

            /*
               User thường:
               Phải chọn quản lý xác nhận.
            */
            if (string.IsNullOrEmpty(request.ConfirmUserCD))
            {
                return Fail("Vui lòng chọn người xác nhận.");
            }

            if (!CanUserConfirmDepartments(request.ConfirmUserCD, departments))
            {
                return Fail("Người xác nhận không có quyền quản lý tất cả bộ phận của nhân viên đã chọn.");
            }

            request.CreateAsFinished = false;
            request.CreateAsManagerAccepted = false;

            return Ok("");
        }

        private List<string> LoadEmployeeDepartments(List<string> employeeCodes)
        {
            if (employeeCodes == null || employeeCodes.Count == 0)
            {
                return new List<string>();
            }

            var parameters = new List<SqlParameter>();
            var names = new List<string>();

            for (int i = 0; i < employeeCodes.Count; i++)
            {
                string name = "@Emp" + i;
                names.Add(name);
                parameters.Add(new SqlParameter(name, employeeCodes[i]));
            }

            string sql = $@"SELECT DISTINCT MaPhongBan FROM [MITACOSQL].[dbo].[NHANVIEN]
                WHERE MaNhanVien IN ({string.Join(",", names)})
                  AND MaPhongBan IS NOT NULL
                  AND MaPhongBan <> ''";

            DataTable dt = SQLHelper.ExecuteDt(sql, parameters.ToArray());

            return dt.AsEnumerable()
                .Select(x => x["MaPhongBan"].ToString())
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
                WHERE MaNhanVien = @MaNhanVien AND AccessLevel BETWEEN 3 AND 5
                  AND BoPhanQuanLy IS NOT NULL
                  AND BoPhanQuanLy <> ''";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@MaNhanVien", userCd));

            var managedDepartments = dt.AsEnumerable()
                .Select(x => x["BoPhanQuanLy"].ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            return departmentCodes.All(dept => managedDepartments.Any(x =>
                    string.Equals(x, dept, StringComparison.OrdinalIgnoreCase)));
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

            if (!CanViewShiftRegisterTicket(tblTicketId))
            {
                TempData["ToastrMessage"] = "Bạn không có quyền xem phiếu đăng ký ca này.";
                TempData["ToastrType"] = "error";

                return RedirectToAction("Index", "Home");
            }

            string sql = @"SELECT t.Id AS TicketId, t.TicketNo, t.TicketTypeId, t.StatusId,
            s.StatusName, t.CreatedUserCD, t.Reason AS TicketReason, h.Id AS HeaderId,
            h.RequestDate, h.FromDate, h.ToDate, h.ConfirmUserCD, cf.TenNhanVien AS ConfirmUserName,
            h.Reason AS ReasonRequest, d.Id AS DetailId, d.EmployeeCD, nv.TenNhanVien AS EmployeeName,
            nv.MaPhongBan, pb.TenPhongBan, d.WorkDate, d.ShiftTypeId, st.ShiftCode, st.ShiftName,
            st.StartTime, st.EndTime FROM tbl_ShiftRegisterHeaders h
            INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
            INNER JOIN mst_TicketStatus s ON t.StatusId = s.StatusId
            INNER JOIN tbl_ShiftRegisterDetails d ON h.Id = d.ShiftRegisterHeaderId
            INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] nv ON d.EmployeeCD = nv.MaNhanVien
            LEFT JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON nv.MaPhongBan = pb.MaPhongBan
            LEFT JOIN [MITACOSQL].[dbo].[NHANVIEN] cf ON h.ConfirmUserCD = cf.MaNhanVien
            INNER JOIN mst_ShiftTypes st ON d.ShiftTypeId = st.ShiftTypeId
            WHERE t.Id = @TicketId AND t.TicketTypeId = @TicketTypeId
            ORDER BY pb.TenPhongBan, nv.MaNhanVien, d.WorkDate";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@TicketId", tblTicketId),
                new SqlParameter("@TicketTypeId", (int)Enums.RequestTypeEnum.SR)
            );

            if (dt.Rows.Count == 0)
            {
                TempData["ToastrMessage"] = "Không tìm thấy phiếu đăng ký ca.";
                TempData["ToastrType"] = "error";

                return RedirectToAction("Index");
            }

            DataRow first = dt.Rows[0];

            var model = new ShiftRegisterDetailPageViewModel
            {
                Ticket = new TblTicketsModel
                {
                    Id = Convert.ToInt32(first["TicketId"]),
                    TicketNo = first["TicketNo"].ToString(),
                    TicketTypeId = Convert.ToInt32(first["TicketTypeId"]),
                    StatusId = Convert.ToInt32(first["StatusId"]),
                    StatusName = first["StatusName"].ToString(),
                    CreatedUserCD = first["CreatedUserCD"].ToString(),
                    Reason = first["TicketReason"] == DBNull.Value ? "" : first["TicketReason"].ToString()
                },

                Header = new ShiftRegisterHeaderDetailViewModel
                {
                    Id = Convert.ToInt32(first["HeaderId"]),
                    TicketId = Convert.ToInt32(first["TicketId"]),
                    RequestDate = Convert.ToDateTime(first["RequestDate"]),
                    FromDate = Convert.ToDateTime(first["FromDate"]),
                    ToDate = Convert.ToDateTime(first["ToDate"]),
                    ConfirmUserCD = first["ConfirmUserCD"] == DBNull.Value ? "" : first["ConfirmUserCD"].ToString(),
                    ConfirmUserName = first["ConfirmUserName"] == DBNull.Value ? "" : first["ConfirmUserName"].ToString(),
                    Reason = first["ReasonRequest"] == DBNull.Value ? "" : first["ReasonRequest"].ToString()
                },

                Details = dt.AsEnumerable()
                    .Select(row => new ShiftRegisterDetailItemViewModel
                    {
                        Id = Convert.ToInt32(row["DetailId"]),
                        ShiftRegisterHeaderId = Convert.ToInt32(row["HeaderId"]),
                        EmployeeCD = row["EmployeeCD"].ToString(),
                        EmployeeName = row["EmployeeName"].ToString(),
                        MaPhongBan = row["MaPhongBan"] == DBNull.Value ? "" : row["MaPhongBan"].ToString(),
                        TenPhongBan = row["TenPhongBan"] == DBNull.Value ? "" : row["TenPhongBan"].ToString(),
                        WorkDate = Convert.ToDateTime(row["WorkDate"]),
                        ShiftTypeId = Convert.ToInt32(row["ShiftTypeId"]),
                        ShiftCode = row["ShiftCode"].ToString(),
                        ShiftName = row["ShiftName"].ToString(),
                        StartTime = (TimeSpan)row["StartTime"],
                        EndTime = (TimeSpan)row["EndTime"]
                    })
                    .ToList()
            };

            ViewBag.CanAdminShiftRegister = CanAdminShiftRegister();
            ViewBag.CanHrProcessShiftRegister = CanAdminShiftRegister();
            ViewBag.CanEditShiftRegister = CanEditShiftRegisterTicket(tblTicketId);

            return View(model);
        }

        private bool CanAdminShiftRegister()
        {
            var user = CurrentUser;

            return _permissionService.CanViewAllData(user);
        }

        private bool CanViewShiftRegisterTicket(int ticketId)
        {
            var user = CurrentUser;

            if (user == null)
            {
                return false;
            }

            if (CanAdminShiftRegister())
            {
                return true;
            }

            string sql = @" SELECT TOP 1 t.CreatedUserCD, h.ConfirmUserCD FROM tbl_Tickets t
            INNER JOIN tbl_ShiftRegisterHeaders h ON t.Id = h.TicketId
            WHERE t.Id = @TicketId AND t.TicketTypeId = @TicketTypeId";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@TicketId", ticketId),
                new SqlParameter("@TicketTypeId", (int)Enums.RequestTypeEnum.SR));

            if (dt.Rows.Count == 0)
            {
                return false;
            }

            string createdUser = dt.Rows[0]["CreatedUserCD"].ToString();
            string confirmUser = dt.Rows[0]["ConfirmUserCD"] == DBNull.Value
                ? string.Empty
                : dt.Rows[0]["ConfirmUserCD"].ToString();

            return string.Equals(createdUser, user.MaNhanVien, StringComparison.OrdinalIgnoreCase)
                || string.Equals(confirmUser, user.MaNhanVien, StringComparison.OrdinalIgnoreCase);
        }

        //public ActionResult Accept(int tblTicketId)
        //{
        //    if (Session["LoginInfo"] == null)
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }

        //    var currentUser = CurrentUser;

        //    if (currentUser == null)
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }

        //    bool canForceApprove = CanManageAllShiftRegister();

        //    var service = new ShiftRegisterService(new ApplicationDbContext());

        //    var result = service.ApproveShiftRegisterRequest(tblTicketId, currentUser.MaNhanVien, true, null, canForceApprove);

        //    if (result.Success)
        //    {
        //        TempData["ToastrMessage"] = result.Message;
        //        TempData["ToastrType"] = "success";

        //        return RedirectToAction("Index", "Home");
        //    }

        //    TempData["ToastrMessage"] = result.Message;
        //    TempData["ToastrType"] = "error";

        //    return RedirectToAction("Detail", new { tblTicketId });
        //}

        public ActionResult ManagerAccept(int tblTicketId)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = CurrentUser;
            var service = new ShiftRegisterService(new ApplicationDbContext());
            var result = service.ManagerAcceptShiftRegisterRequest(tblTicketId, currentUser.MaNhanVien, false);

            TempData["ToastrMessage"] = result.Message;
            TempData["ToastrType"] = result.Success ? "success" : "error";

            return RedirectToAction("Detail", new { tblTicketId });
        }

        //public ActionResult Reject(int tblTicketId, string reason)
        //{
        //    if (Session["LoginInfo"] == null)
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }

        //    var currentUser = CurrentUser;

        //    if (currentUser == null)
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }

        //    bool canForceApprove = CanManageAllShiftRegister();

        //    var service = new ShiftRegisterService(new ApplicationDbContext());

        //    var result = service.ApproveShiftRegisterRequest(tblTicketId, currentUser.MaNhanVien, false, reason, canForceApprove);

        //    if (result.Success)
        //    {
        //        TempData["ToastrMessage"] = result.Message;
        //        TempData["ToastrType"] = "success";

        //        return RedirectToAction("Index", "Home");
        //    }

        //    TempData["ToastrMessage"] = result.Message;
        //    TempData["ToastrType"] = "error";

        //    return RedirectToAction("Detail", new { tblTicketId });
        //}

        public ActionResult ManagerReject(int tblTicketId, string reason)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = CurrentUser;
            var service = new ShiftRegisterService(new ApplicationDbContext());
            var result = service.ManagerRejectShiftRegisterRequest(tblTicketId, currentUser.MaNhanVien, reason, false);

            TempData["ToastrMessage"] = result.Message;
            TempData["ToastrType"] = result.Success ? "success" : "error";

            return RedirectToAction("Detail", new { tblTicketId });
        }

        public ActionResult HrFinish(int tblTicketId)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = CurrentUser;

            bool canHrProcess = CanAdminShiftRegister();

            var service = new ShiftRegisterService(new ApplicationDbContext());

            var result = service.HrFinishShiftRegisterRequest(
                tblTicketId,
                currentUser.MaNhanVien,
                canHrProcess
            );

            TempData["ToastrMessage"] = result.Message;
            TempData["ToastrType"] = result.Success ? "success" : "error";

            return RedirectToAction("Detail", new { tblTicketId });
        }

        public ActionResult HrReject(int tblTicketId, string reason)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = CurrentUser;
            bool canHrProcess = CanAdminShiftRegister();
            var service = new ShiftRegisterService(new ApplicationDbContext());
            var result = service.HrRejectShiftRegisterRequest(tblTicketId, currentUser.MaNhanVien, reason, canHrProcess);

            TempData["ToastrMessage"] = result.Message;
            TempData["ToastrType"] = result.Success ? "success" : "error";

            return RedirectToAction("Detail", new { tblTicketId });
        }

        public ActionResult Cancel(int tblTicketId)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = CurrentUser;

            bool canCancel = CanAdminShiftRegister();

            var service = new ShiftRegisterService(new ApplicationDbContext());

            var result = service.CancelShiftRegisterRequest(tblTicketId, currentUser.MaNhanVien, canCancel);

            TempData["ToastrMessage"] = result.Message;
            TempData["ToastrType"] = result.Success ? "success" : "error";

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

            if (currentUser == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Phiên đăng nhập đã hết hạn."
                });
            }

            bool canForceModify = CanAdminShiftRegister();

            var ticket = _db.TblTickets.FirstOrDefault(t =>
                t.Id == ticketID &&
                t.TicketTypeId == (int)Enums.RequestTypeEnum.SR);

            if (ticket == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy phiếu đăng ký ca."
                });
            }

            var header = _db.TblShiftRegisterHeaders.FirstOrDefault(h =>
                h.Id == headerID &&
                h.TicketId == ticketID);

            if (header == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin đăng ký ca."
                });
            }

            bool isCreator = string.Equals(ticket.CreatedUserCD, currentUser.MaNhanVien, StringComparison.OrdinalIgnoreCase);

            bool isConfirmUser = string.Equals(header.ConfirmUserCD, currentUser.MaNhanVien, StringComparison.OrdinalIgnoreCase);

            bool isPending = ticket.StatusId == (int)Enums.RequestStatusEnum.Pending;

            if (canForceModify == false)
            {
                if (!isPending)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiếu đã được xử lý, bạn không thể xoá nhân viên trong phiếu."
                    });
                }

                if (isCreator == false && isConfirmUser == false)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bạn không có quyền xoá nhân viên trong phiếu này."
                    });
                }
            }

            var details = _db.TblShiftRegisterDetails
                .Where(d =>
                    d.EmployeeCD == employeeCD &&
                    d.ShiftRegisterHeaderId == headerID)
                .ToList();

            if (details.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy nhân viên trong phiếu."
                });
            }

            _db.TblShiftRegisterDetails.RemoveRange(details);
            _db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Đã xoá nhân viên khỏi phiếu đăng ký ca."
            });
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

            if (!CanEditShiftRegisterTicket(tblTicketId))
            {
                TempData["ToastrMessage"] = "Bạn không có quyền chỉnh sửa phiếu đăng ký ca này.";
                TempData["ToastrType"] = "error";

                return RedirectToAction("Detail", new { tblTicketId });
            }

            string sql = @"SELECT t.Id AS TicketId, t.TicketNo, t.StatusId, t.CreatedUserCD, h.Id AS HeaderId, h.FromDate, 
            h.ToDate, h.ConfirmUserCD, h.Reason, d.EmployeeCD, nv.TenNhanVien, nv.MaPhongBan, pb.TenPhongBan, d.ShiftTypeId
            FROM tbl_ShiftRegisterHeaders h
            INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
            INNER JOIN tbl_ShiftRegisterDetails d ON h.Id = d.ShiftRegisterHeaderId
            INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] nv ON d.EmployeeCD = nv.MaNhanVien
            LEFT JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON nv.MaPhongBan = pb.MaPhongBan
            WHERE t.Id = @TicketId AND t.TicketTypeId = @TicketTypeId
            ORDER BY pb.TenPhongBan, nv.MaNhanVien";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@TicketId", tblTicketId),
                new SqlParameter("@TicketTypeId", (int)Enums.RequestTypeEnum.SR));

            if (dt.Rows.Count == 0)
            {
                TempData["ToastrMessage"] = "Không tìm thấy phiếu đăng ký ca.";
                TempData["ToastrType"] = "error";

                return RedirectToAction("Index");
            }

            DataRow first = dt.Rows[0];

            var model = new ShiftRegisterPageViewModel
            {
                Employees = LoadEmployees(),
                UserRoles = LoadConfirmUsers(),
                ShiftTypes = LoadShiftTypes(),

                Request = new ShiftRegisterRequestModel
                {
                    TicketId = Convert.ToInt32(first["TicketId"]),
                    HeaderId = Convert.ToInt32(first["HeaderId"]),
                    FromDate = Convert.ToDateTime(first["FromDate"]),
                    ToDate = Convert.ToDateTime(first["ToDate"]),
                    ConfirmUserCD = first["ConfirmUserCD"] == DBNull.Value ? "" : first["ConfirmUserCD"].ToString(),
                    Reason = first["Reason"] == DBNull.Value ? "" : first["Reason"].ToString(),
                    ShiftTypeId = Convert.ToInt32(first["ShiftTypeId"]),

                    EmployeeCDs = dt.AsEnumerable()
                        .Select(row => new EmployeeModel
                        {
                            MaNhanVien = row["EmployeeCD"].ToString(),
                            TenNhanVien = row["TenNhanVien"].ToString(),
                            MaPhongBan = row["MaPhongBan"] == DBNull.Value ? "" : row["MaPhongBan"].ToString(),
                            TenPhongBan = row["TenPhongBan"] == DBNull.Value ? "" : row["TenPhongBan"].ToString()
                        })
                        .GroupBy(x => x.MaNhanVien)
                        .Select(g => g.First())
                        .ToList()
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(ShiftRegisterPageViewModel model)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (model.Request == null || model.Request.TicketId <= 0 || model.Request.HeaderId <= 0)
            {
                TempData["ToastrMessage"] = "Dữ liệu phiếu không hợp lệ.";
                TempData["ToastrType"] = "error";

                return RedirectToAction("Index");
            }

            if (!CanEditShiftRegisterTicket(model.Request.TicketId))
            {
                TempData["ToastrMessage"] = "Bạn không có quyền chỉnh sửa phiếu đăng ký ca này.";
                TempData["ToastrType"] = "error";

                return RedirectToAction("Detail", new { tblTicketId = model.Request.TicketId });
            }

            if (model.Request.EmployeeCDs == null || !model.Request.EmployeeCDs.Any(x => !string.IsNullOrEmpty(x.MaNhanVien)))
            {
                TempData["ToastrMessage"] = "Bạn phải chọn ít nhất 1 nhân viên đăng ký ca.";
                TempData["ToastrType"] = "error";

                ReloadPageData(model);

                return View("Edit", model);
            }

            var employeeCodes = model.Request.EmployeeCDs
                .Where(x => !string.IsNullOrEmpty(x.MaNhanVien))
                .Select(x => x.MaNhanVien)
                .Distinct()
                .ToList();

            var departmentValidation = ValidateDepartmentsCanUseShiftRegister(employeeCodes, model.Request.FromDate, model.Request.ToDate);

            if (!departmentValidation.Success)
            {
                TempData["ToastrMessage"] = departmentValidation.Message;
                TempData["ToastrType"] = "error";

                ReloadPageData(model);

                return View("Edit", model);
            }

            var statusResult = PrepareStatusForSubmit(model.Request);

            if (!statusResult.Success)
            {
                TempData["ToastrMessage"] = statusResult.Message;
                TempData["ToastrType"] = "error";

                ReloadPageData(model);

                return View("Index", model);
            }

            model.Request.CreatedUserCD = CurrentUser.MaNhanVien;

            var service = new ShiftRegisterService(new ApplicationDbContext());

            var result = service.UpdateShiftRegisterRequest(model.Request, CanEditShiftRegisterTicket(model.Request.TicketId));

            if (result.Success)
            {
                TempData["ToastrMessage"] = result.Message;
                TempData["ToastrType"] = "success";

                return RedirectToAction("Detail", new { tblTicketId = model.Request.TicketId });
            }

            TempData["ToastrMessage"] = result.Message;
            TempData["ToastrType"] = "error";

            ReloadPageData(model);

            return View("Edit", model);
        }

        private bool CanEditShiftRegisterTicket(int ticketId)
        {
            var user = CurrentUser;

            if (user == null)
            {
                return false;
            }

            if (CanAdminShiftRegister())
            {
                return true;
            }

            string sql = @"SELECT TOP 1 t.CreatedUserCD, t.StatusId FROM tbl_Tickets t
            WHERE t.Id = @TicketId AND t.TicketTypeId = @TicketTypeId";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@TicketId", ticketId),
                new SqlParameter("@TicketTypeId", (int)Enums.RequestTypeEnum.SR));

            if (dt.Rows.Count == 0)
            {
                return false;
            }

            string createdUser = dt.Rows[0]["CreatedUserCD"].ToString();
            int statusId = Convert.ToInt32(dt.Rows[0]["StatusId"]);

            bool isCreator = string.Equals(createdUser, user.MaNhanVien, StringComparison.OrdinalIgnoreCase);

            bool isPending = statusId == (int)Enums.RequestStatusEnum.Pending;

            if (isCreator && isPending)
            {
                return true;
            }

            var ticketDepartments = LoadTicketDepartments(ticketId);

            if (ticketDepartments.Count == 0)
            {
                return false;
            }

            return CanUserConfirmDepartments(user.MaNhanVien, ticketDepartments);
        }

        private List<string> LoadTicketDepartments(int ticketId)
        {
            string sql = @"SELECT DISTINCT nv.MaPhongBan
            FROM tbl_ShiftRegisterHeaders h
            INNER JOIN tbl_ShiftRegisterDetails d ON h.Id = d.ShiftRegisterHeaderId
            INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] nv ON d.EmployeeCD = nv.MaNhanVien
            WHERE h.TicketId = @TicketId AND nv.MaPhongBan IS NOT NULL AND nv.MaPhongBan <> ''";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@TicketId", ticketId));

            return dt.AsEnumerable()
                .Select(x => x["MaPhongBan"].ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();
        }

        private ServiceResult Ok(string message)
        {
            return new ServiceResult
            {
                Success = true,
                Message = message
            };
        }

        private ServiceResult Fail(string message)
        {
            return new ServiceResult
            {
                Success = false,
                Message = message
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}