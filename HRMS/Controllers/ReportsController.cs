using HRMS.Common;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.Services;
using HRMS.ViewModels;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace HRMS.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly PermissionService _permissionService;

        // GET: Reports
        public ReportsController()
        {
            _db = new ApplicationDbContext();
            _permissionService = new PermissionService();
        }

        public ActionResult Index()
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // GET: Reports/Attendance
        public ActionResult Attendance(string month, string dept, string employee, string status)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            string selectedMonth = !string.IsNullOrEmpty(month) ? month : DateTime.Now.ToString("yyyy-MM");
            var currentUser = Session["LoginInfo"] as UsersModel;
            var model = BuildAttendanceReport(selectedMonth, dept, employee, status, currentUser);
            return View(model);
        }

        [HttpGet]
        public JsonResult AttendanceData(string month, string dept, string employee, string status)
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
                var currentUser = Session["LoginInfo"] as UsersModel;
                string selectedMonth = !string.IsNullOrEmpty(month) ? month : DateTime.Now.ToString("yyyy-MM");

                var model = BuildAttendanceReport(selectedMonth, dept, employee, status, currentUser);

                var jsonResult = Json(new
                {
                    success = true,
                    summary = new
                    {
                        totalEmployees = model.TotalEmployees,
                        totalLateIn = model.TotalLateIn,
                        totalEarlyOut = model.TotalEarlyOut,
                        totalMissing = model.TotalMissing,
                        totalWorkOnOffDay = model.TotalWorkOnOffDay
                    },
                    rows = model.Rows.Select(r => new
                    {
                        r.MaPhongBan,
                        r.TenPhongBan,
                        r.EmployeeCD,
                        r.TenNhanVien,
                        r.WorkingDays,
                        r.TotalHours,
                        r.IssueCount,
                        Days = r.Days.Select(d => d == null ? null : new
                        {
                            d.Day,
                            Date = d.Date.ToString("yyyy-MM-dd"),
                            FirstCheckIn = d.FirstCheckIn.HasValue ? d.FirstCheckIn.Value.ToString("HH:mm") : "",
                            LastCheckOut = d.LastCheckOut.HasValue ? d.LastCheckOut.Value.ToString("HH:mm") : "",
                            d.WorkingHours,
                            d.IsOffDay,
                            d.IsHoliday,
                            d.StatusCode,
                            d.StatusText,
                            d.Symbol,
                            d.Note,
                            d.ShiftCode,
                            d.ShiftName,
                            d.ShiftSource,
                            d.ShiftTimeText
                        }).ToList()
                    })
                }, JsonRequestBehavior.AllowGet);

                jsonResult.MaxJsonLength = int.MaxValue;

                return jsonResult;
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

        private AttendanceReportPageViewModel BuildAttendanceReport(string month, string dept, string employee,
            string status, UsersModel usersModel)
        {
            DateTime selectedDate;

            if (!DateTime.TryParse(month + "-01", out selectedDate))
            {
                selectedDate = DateTime.Now;
            }

            int monthValue = selectedDate.Month;
            int yearValue = selectedDate.Year;

            var rows = GetAttendanceRows(monthValue, yearValue, dept, employee, usersModel);

            if (!string.IsNullOrEmpty(status))
            {
                rows = rows
                    .Where(r => r.Days.Any(d =>
                        d != null &&
                        d.StatusCode == status))
                    .ToList();
            }

            return new AttendanceReportPageViewModel
            {
                SelectedMonth = selectedDate.ToString("yyyy-MM"),
                SelectedDept = dept,
                SelectedEmployee = employee,
                SelectedStatus = status,
                Departments = PermissionScopeHelper.LoadDepartmentsForUser(usersModel, _db, _permissionService),
                Rows = rows,

                TotalEmployees = rows.Count,
                TotalLateIn = rows.Sum(r => r.Days.Count(d => d != null && d.StatusCode == AttendanceStatusCode.LateIn)),
                TotalEarlyOut = rows.Sum(r => r.Days.Count(d => d != null && d.StatusCode == AttendanceStatusCode.EarlyOut)),
                TotalMissing = rows.Sum(r => r.Days.Count(d => d != null
                    && (d.StatusCode == AttendanceStatusCode.MissingIn
                    || d.StatusCode == AttendanceStatusCode.MissingOut
                    || d.StatusCode == AttendanceStatusCode.NoData))),
                TotalWorkOnOffDay = rows.Sum(r => r.Days.Count(d => d != null && d.StatusCode == AttendanceStatusCode.WorkOnOffDay))
            };
        }

        private List<AttendanceEmployeeRowViewModel> GetAttendanceRows(int month, int year, string dept, string employee, UsersModel usersModel)
        {
            var employees = LoadAttendanceEmployees(dept, employee, usersModel);
            var checkData = LoadCheckInOut(month, year, dept, employee, usersModel);
            var offDays = LoadAttendanceOffDays(month, year);

            var shiftData = LoadEffectiveShiftsForMonth(employees, month, year);

            int daysInMonth = DateTime.DaysInMonth(year, month);

            var result = new List<AttendanceEmployeeRowViewModel>();

            foreach (var emp in employees)
            {
                var row = new AttendanceEmployeeRowViewModel
                {
                    MaPhongBan = emp.MaPhongBan,
                    TenPhongBan = emp.TenPhongBan,
                    EmployeeCD = emp.EmployeeCD,
                    TenNhanVien = emp.TenNhanVien
                };

                for (int day = 1; day <= 31; day++)
                {
                    if (day > daysInMonth)
                    {
                        row.Days[day - 1] = new AttendanceDayCellViewModel
                        {
                            Day = day,
                            StatusCode = AttendanceStatusCode.Empty,
                            StatusText = "",
                            Symbol = "",
                            Note = ""
                        };

                        continue;
                    }

                    DateTime date = new DateTime(year, month, day);

                    string key = emp.EmployeeCD + "|" + date.ToString("yyyy-MM-dd");

                    AttendanceRawItem att = checkData.ContainsKey(key) ? checkData[key] : null;

                    var offInfo = offDays.ContainsKey(day)
                        ? offDays[day]
                        : new AttendanceOffDayItem
                        {
                            IsOff = false,
                            IsHoliday = false,
                            Note = "Ngày làm việc"
                        };

                    EffectiveShiftViewModel effectiveShift = shiftData.ContainsKey(key) ? shiftData[key] : null;

                    var cell = BuildAttendanceCell(day, date, att, offInfo, effectiveShift);

                    row.Days[day - 1] = cell;
                }

                row.WorkingDays = row.Days.Count(d => d != null && AttendanceStatusHelper.IsWorkingStatus(d.StatusCode));
                row.TotalHours = row.Days.Sum(d => d != null ? d.WorkingHours : 0);
                row.IssueCount = row.Days.Count(d => d != null && AttendanceStatusHelper.IsIssue(d.StatusCode));

                result.Add(row);
            }

            return result.OrderBy(x => x.TenPhongBan).ThenBy(x => x.EmployeeCD).ToList();
        }

        private Dictionary<string, EffectiveShiftViewModel> LoadEffectiveShiftsForMonth(
            List<AttendanceEmployeeItem> employees, int month, int year)
        {
            var result = new Dictionary<string, EffectiveShiftViewModel>();

            if (employees == null || employees.Count == 0)
            {
                return result;
            }

            DateTime fromDate = new DateTime(year, month, 1);
            DateTime toDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            var employeeCodes = employees
                .Where(x => !string.IsNullOrEmpty(x.EmployeeCD))
                .Select(x => x.EmployeeCD)
                .Distinct()
                .ToList();

            if (employeeCodes.Count == 0)
            {
                return result;
            }

            var parameters = new List<SqlParameter>();
            var employeeParams = new List<string>();

            for (int i = 0; i < employeeCodes.Count; i++)
            {
                string paramName = "@Emp" + i;

                employeeParams.Add(paramName);
                parameters.Add(new SqlParameter(paramName, employeeCodes[i]));
            }

            parameters.Add(new SqlParameter("@FromDate", fromDate.Date));
            parameters.Add(new SqlParameter("@ToDate", toDate.Date));
            parameters.Add(new SqlParameter("@TicketTypeId", (int)Enums.RequestTypeEnum.SR));
            parameters.Add(new SqlParameter("@AcceptedStatus", (int)Enums.RequestStatusEnum.ManagerAccepted));

            string employeeInSql = string.Join(",", employeeParams);

            /*
               1. Lấy ca đăng ký đã duyệt.
               Ưu tiên cao nhất.
            */

            string registeredEmployeeInSql = string.Empty;
            var registeredParameters = BuildEmployeeSqlParameters(employeeCodes,out registeredEmployeeInSql);
            registeredParameters.Add(new SqlParameter("@FromDate", fromDate.Date));
            registeredParameters.Add(new SqlParameter("@ToDate", toDate.Date));
            registeredParameters.Add(new SqlParameter("@TicketTypeId", (int)Enums.RequestTypeEnum.SR));
            registeredParameters.Add(new SqlParameter("@AcceptedStatus", (int)Enums.RequestStatusEnum.ManagerAccepted));

            string registeredSql = $@"SELECT d.EmployeeCD, CAST(d.WorkDate AS date) AS WorkDate, st.ShiftTypeId, 
            st.ShiftCode, st.ShiftName, st.StartTime, st.EndTime, st.BreakMinutes, st.IsNightShift
            FROM tbl_ShiftRegisterDetails d
            INNER JOIN tbl_ShiftRegisterHeaders h ON d.ShiftRegisterHeaderId = h.Id
            INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
            INNER JOIN mst_ShiftTypes st ON d.ShiftTypeId = st.ShiftTypeId
            WHERE d.EmployeeCD IN ({registeredEmployeeInSql})
              AND CAST(d.WorkDate AS date) BETWEEN @FromDate AND @ToDate
              AND t.TicketTypeId = @TicketTypeId
              AND t.StatusId = @AcceptedStatus
            ORDER BY d.EmployeeCD, d.WorkDate, d.Id DESC";

            DataTable registeredDt = SQLHelper.ExecuteDt(registeredSql, registeredParameters.ToArray());

            foreach (DataRow row in registeredDt.Rows)
            {
                string emp = row["EmployeeCD"].ToString();
                DateTime workDate = Convert.ToDateTime(row["WorkDate"]);

                string key = emp + "|" + workDate.ToString("yyyy-MM-dd");

                result[key] = MapEffectiveShift(row, "REGISTERED");
            }

            /*
               2. Lấy ca mặc định theo phòng ban.
               Chỉ fill vào ngày chưa có ca đăng ký.
            */

            string defaultSql = $@"SELECT nv.MaNhanVien AS EmployeeCD, ds.EffectiveFrom,
            ds.EffectiveTo, st.ShiftTypeId, st.ShiftCode, st.ShiftName, st.StartTime,
            st.EndTime, st.BreakMinutes, st.IsNightShift
            FROM [MITACOSQL].[dbo].[NHANVIEN] nv
            INNER JOIN [TIME_KEEPING].[dbo].[mst_DefaultShifts] ds ON nv.MaPhongBan = ds.DepartmentCD
            INNER JOIN [TIME_KEEPING].[dbo].[mst_ShiftTypes] st ON ds.ShiftTypeId = st.ShiftTypeId
            WHERE nv.MaNhanVien IN ({employeeInSql})
              AND ds.IsActive = 1
              AND ds.EffectiveFrom <= @ToDate
              AND (
                    ds.EffectiveTo IS NULL
                    OR ds.EffectiveTo >= @FromDate
                  )
            ORDER BY nv.MaNhanVien, ds.EffectiveFrom DESC, ds.Id DESC";

            DataTable defaultDt = SQLHelper.ExecuteDt(defaultSql, parameters.ToArray());

            foreach (DataRow row in defaultDt.Rows)
            {
                string emp = row["EmployeeCD"].ToString();

                DateTime effectiveFrom = Convert.ToDateTime(row["EffectiveFrom"]);

                DateTime effectiveTo = row["EffectiveTo"] == DBNull.Value ? toDate : Convert.ToDateTime(row["EffectiveTo"]);

                DateTime start = effectiveFrom > fromDate ? effectiveFrom.Date : fromDate.Date;

                DateTime end = effectiveTo < toDate ? effectiveTo.Date : toDate.Date;

                for (DateTime date = start; date <= end; date = date.AddDays(1))
                {
                    string key = emp + "|" + date.ToString("yyyy-MM-dd");

                    if (result.ContainsKey(key))
                    {
                        continue;
                    }

                    result[key] = MapEffectiveShift(row, "DEFAULT_DEPARTMENT");
                }
            }

            return result;
        }

        private List<SqlParameter> BuildEmployeeSqlParameters(List<string> employeeCodes, out string employeeInSql)
        {
            var parameters = new List<SqlParameter>();
            var parameterNames = new List<string>();

            for (int i = 0; i < employeeCodes.Count; i++)
            {
                string paramName = "@Emp" + i;

                parameterNames.Add(paramName);
                parameters.Add(new SqlParameter(paramName, employeeCodes[i]));
            }

            employeeInSql = string.Join(",", parameterNames);

            return parameters;
        }

        private EffectiveShiftViewModel MapEffectiveShift(DataRow row, string source)
        {
            return new EffectiveShiftViewModel
            {
                ShiftTypeId = Convert.ToInt32(row["ShiftTypeId"]),
                ShiftCode = row["ShiftCode"].ToString(),
                ShiftName = row["ShiftName"].ToString(),
                StartTime = (TimeSpan)row["StartTime"],
                EndTime = (TimeSpan)row["EndTime"],
                BreakMinutes = row["BreakMinutes"] == DBNull.Value ? 0 : Convert.ToInt32(row["BreakMinutes"]),
                IsNightShift = row["IsNightShift"] != DBNull.Value && Convert.ToBoolean(row["IsNightShift"]),
                Source = source
            };
        }

        private Dictionary<string, AttendanceRawItem> LoadCheckInOut(int month, int year, string dept, string employee, UsersModel currentUser)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Month", month),
                new SqlParameter("@Year", year),
                new SqlParameter("@Dept", string.IsNullOrEmpty(dept) ? (object)DBNull.Value : dept),
                new SqlParameter("@Employee", string.IsNullOrEmpty(employee) ? (object)DBNull.Value : employee)
            };
            string permissionWhere = PermissionScopeHelper.BuildEmployeeScopeWhere(currentUser, _permissionService, dept,
                "nv", "nv.MaPhongBan", parameters);

            if (permissionWhere == "NO_ACCESS")
            {
                return new Dictionary<string, AttendanceRawItem>();
            }

            string sql = @"SELECT nv.MaNhanVien, CAST(c.NgayCham AS date) AS Ngay, COUNT(*) AS PunchCount, 
            MIN(c.GioCham) AS FirstCheckIn, MAX(c.GioCham) AS LastCheckOut FROM NhanVien nv
            INNER JOIN PhongBan pb ON nv.MaPhongBan = pb.MaPhongBan
            INNER JOIN [MITACOSQL].[dbo].[CheckInOut] c ON c.MaChamCong = nv.MaChamCong
            WHERE MONTH(c.NgayCham) = @Month AND YEAR(c.NgayCham) = @Year AND (@Dept IS NULL OR pb.MaPhongBan = @Dept)
            AND (
                @Employee IS NULL
                OR nv.MaNhanVien LIKE '%' + @Employee + '%'
                OR nv.TenNhanVien LIKE N'%' + @Employee + N'%'
            )";
            sql += permissionWhere;

            sql += @" GROUP BY nv.MaNhanVien, CAST(c.NgayCham AS date)";

            DataTable dt = SQLHelper.ExecuteDt(sql, parameters.ToArray());

            var dict = new Dictionary<string, AttendanceRawItem>();

            foreach (DataRow dr in dt.Rows)
            {
                string emp = dr["MaNhanVien"].ToString();

                DateTime date = Convert.ToDateTime(dr["Ngay"]);

                string key = emp + "|" + date.ToString("yyyy-MM-dd");

                dict[key] = new AttendanceRawItem
                {
                    PunchCount = Convert.ToInt32(dr["PunchCount"]),
                    FirstCheckIn = dr["FirstCheckIn"] != DBNull.Value
                        ? Convert.ToDateTime(dr["FirstCheckIn"])
                        : (DateTime?)null,
                    LastCheckOut = dr["LastCheckOut"] != DBNull.Value
                        ? Convert.ToDateTime(dr["LastCheckOut"])
                        : (DateTime?)null
                };
            }

            return dict;
        }

        private Dictionary<int, AttendanceOffDayItem> LoadAttendanceOffDays(int month, int year)
        {
            string sql = @"
            SELECT 
                a.Nam,
                a.Thang,
                a.D1,a.D2,a.D3,a.D4,a.D5,a.D6,a.D7,a.D8,a.D9,a.D10,
                a.D11,a.D12,a.D13,a.D14,a.D15,a.D16,a.D17,a.D18,a.D19,a.D20,
                a.D21,a.D22,a.D23,a.D24,a.D25,a.D26,a.D27,a.D28,a.D29,a.D30,a.D31,
                b.Day,
                b.Note
            FROM NgayNghi a
            LEFT JOIN tbl_Holiday b
                ON a.Nam = b.Year
               AND a.Thang = b.Month
            WHERE a.Nam = @Year
              AND a.Thang = @Month";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@Year", year),
                new SqlParameter("@Month", month));

            var result = new Dictionary<int, AttendanceOffDayItem>();
            int daysInMonth = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= daysInMonth; day++)
            {
                bool isOff = false;
                bool isHoliday = false;
                string note = "";

                if (dt.Rows.Count > 0)
                {
                    string col = "D" + day;

                    if (dt.Columns.Contains(col) && dt.Rows[0][col] != DBNull.Value)
                    {
                        isOff = Convert.ToBoolean(dt.Rows[0][col]);
                    }

                    foreach (DataRow r in dt.Rows)
                    {
                        if (r["Day"] != DBNull.Value && r["Day"].ToString() == day.ToString())
                        {
                            isHoliday = true;
                            note = r["Note"] != DBNull.Value ? r["Note"].ToString() : "Ngày lễ";
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(note))
                {
                    note = isOff ? "Ngày nghỉ" : "Ngày làm việc";
                }

                result[day] = new AttendanceOffDayItem
                {
                    IsOff = isOff,
                    IsHoliday = isHoliday,
                    Note = note
                };
            }

            return result;
        }

        private AttendanceDayCellViewModel BuildAttendanceCell(int day, DateTime date, AttendanceRawItem att,
            AttendanceOffDayItem offInfo, EffectiveShiftViewModel shift)
        {
            DateTime? first = att?.FirstCheckIn;
            DateTime? last = att?.LastCheckOut;

            int punchCount = att != null ? att.PunchCount : 0;

            decimal hours = 0;

            if (first.HasValue && last.HasValue && last.Value > first.Value)
            {
                hours = Math.Round(Convert.ToDecimal((last.Value - first.Value).TotalHours), 2);
            }

            string code = string.Empty;
            string note = offInfo.Note;

            /*
               Nếu không có ca hiệu lực:
               Fallback về giờ hành chính để không làm vỡ báo cáo cũ.
               Sau này nếu muốn chặt hơn thì đổi sang trạng thái "Không xác định ca".
            */
            TimeSpan standardIn = shift != null ? shift.StartTime : new TimeSpan(7, 50, 59);

            TimeSpan standardOut = shift != null ? shift.EndTime : new TimeSpan(16, 30, 0);

            string shiftName = shift != null ? shift.ShiftName : "Ca hành chính";

            string shiftCode = shift != null ? shift.ShiftCode : "HC";

            string shiftSource = shift != null ? shift.Source : "FALLBACK";

            string shiftTimeText = standardIn.ToString(@"hh\:mm") + " - " + standardOut.ToString(@"hh\:mm");

            if (offInfo.IsHoliday && punchCount == 0)
            {
                code = AttendanceStatusCode.Holiday;
            }
            else if (offInfo.IsOff && punchCount == 0)
            {
                code = AttendanceStatusCode.Off;
            }
            else if (offInfo.IsOff && punchCount > 0)
            {
                code = AttendanceStatusCode.WorkOnOffDay;
                note = BuildNote(first, last, AttendanceStatusHelper.GetText(code), shiftName, shiftTimeText);
            }
            else if (punchCount == 0 && date.Date <= DateTime.Today)
            {
                code = AttendanceStatusCode.NoData;
            }
            else if (punchCount == 1)
            {
                code = AttendanceStatusCode.MissingOut;
                note = BuildNote(first, last, AttendanceStatusHelper.GetText(code), shiftName, shiftTimeText);
            }
            else if (first.HasValue && IsLateIn(first.Value.TimeOfDay, standardIn))
            {
                code = AttendanceStatusCode.LateIn;
                note = BuildNote(first, last, AttendanceStatusHelper.GetText(code), shiftName, shiftTimeText);
            }
            else if (last.HasValue && IsEarlyOut(last.Value.TimeOfDay, standardOut))
            {
                code = AttendanceStatusCode.EarlyOut;
                note = BuildNote(first, last, AttendanceStatusHelper.GetText(code), shiftName, shiftTimeText);
            }
            else if (punchCount > 0)
            {
                code = AttendanceStatusCode.Ok;
                note = BuildNote(first, last, AttendanceStatusHelper.GetText(code), shiftName, shiftTimeText);
            }
            else
            {
                code = AttendanceStatusCode.Future;
            }

            return new AttendanceDayCellViewModel
            {
                Day = day,
                Date = date,
                FirstCheckIn = first,
                LastCheckOut = last,
                WorkingHours = hours,
                IsOffDay = offInfo.IsOff,
                IsHoliday = offInfo.IsHoliday,
                StatusCode = code,
                StatusText = AttendanceStatusHelper.GetText(code),
                Symbol = AttendanceStatusHelper.GetSymbol(code),
                Note = note,

                ShiftTypeId = shift?.ShiftTypeId,
                ShiftCode = shiftCode,
                ShiftName = shiftName,
                ShiftSource = shiftSource,
                ShiftTimeText = shiftTimeText
            };
        }

        private string BuildNote(DateTime? first, DateTime? last, string statusText, string shiftName, string shiftTimeText)
        {
            string firstText = first.HasValue ? first.Value.ToString("HH:mm") : "--";

            string lastText = last.HasValue ? last.Value.ToString("HH:mm") : "--";

            return $"Ca: {shiftName} ({shiftTimeText}) | Vào: {firstText} | Ra: {lastText} | {statusText}";
        }

        private bool IsLateIn(TimeSpan actualIn, TimeSpan standardIn)
        {
            /*
               Cho phép đi sau chuẩn là trễ.
               Nếu sau này muốn grace time 5 phút, sửa:
               return actualIn > standardIn.Add(TimeSpan.FromMinutes(5));
            */
            return actualIn > standardIn;
        }

        private bool IsEarlyOut(TimeSpan actualOut, TimeSpan standardOut)
        {
            /*
               Ca đêm có EndTime nhỏ hơn StartTime, ví dụ 22:00 - 06:00.
               Do hiện tại LastCheckOut đang lấy TimeOfDay trong cùng ngày chấm,
               ca đêm cần xử lý riêng ở version sau nếu dữ liệu NgayCham qua ngày.
            */
            return actualOut < standardOut;
        }

        // GET: Reports/ExportOvertime
        public ActionResult ExportOvertime(string month, string dept, string employee)
        {
            var currentUser = Session["LoginInfo"] as UsersModel;
            var data = GetOvertimeData(month, dept, employee, currentUser);
            string templatePath = Server.MapPath("~/Resources/Report.xlsx");
            IWorkbook workbook;

            using (var fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(fs);
            }

            ISheet sheet = workbook.GetSheetAt(0);

            #region Title
            DateTime selectedMonth;

            if (!DateTime.TryParse(month + "-01", out selectedMonth))
            {
                selectedMonth = DateTime.Now;
            }

            sheet.GetRow(0).GetCell(0).SetCellValue($"BÁO CÁO THỐNG KÊ TĂNG CA THÁNG {selectedMonth:MM/yyyy}");
            #endregion

            #region Styles
            IDataFormat dataFormat = workbook.CreateDataFormat();
            // Font chung
            IFont normalFont = workbook.CreateFont();
            normalFont.FontName = "Times New Roman";
            normalFont.FontHeightInPoints = 13;

            // Border thường
            ICellStyle borderStyle = workbook.CreateCellStyle();

            borderStyle.BorderTop = BorderStyle.Thin;
            borderStyle.BorderBottom = BorderStyle.Thin;
            borderStyle.BorderLeft = BorderStyle.Thin;
            borderStyle.BorderRight = BorderStyle.Thin;
            borderStyle.Alignment = HorizontalAlignment.Center;
            borderStyle.VerticalAlignment = VerticalAlignment.Center;
            borderStyle.SetFont(normalFont);

            // Decimal
            ICellStyle decimalStyle = workbook.CreateCellStyle();
            decimalStyle.CloneStyleFrom(borderStyle);
            decimalStyle.DataFormat = dataFormat.GetFormat("0.00");

            // Total Row
            ICellStyle totalStyle = workbook.CreateCellStyle();
            totalStyle.CloneStyleFrom(decimalStyle);
            totalStyle.FillForegroundColor = IndexedColors.LightYellow.Index;
            totalStyle.FillPattern = FillPattern.SolidForeground;


            IFont boldFont = workbook.CreateFont();
            boldFont.FontName = "Times New Roman";
            boldFont.FontHeightInPoints = 13;
            boldFont.IsBold = true;
            totalStyle.SetFont(boldFont);
            totalStyle.Alignment = HorizontalAlignment.Center;
            totalStyle.VerticalAlignment = VerticalAlignment.Center;
            #endregion

            int currentRow = 2; // Excel Row 3

            foreach (var item in data.Where(x => !x.IsTotalRow))
            {
                IRow row = sheet.GetRow(currentRow) ?? sheet.CreateRow(currentRow);
                // A
                SetCell(row, 0, item.EmployeeCD, borderStyle);
                // B
                SetCell(row, 1, item.TenNhanVien, borderStyle);
                // C
                SetCell(row, 2, item.TenPhongBan, borderStyle);
                // Ngày 1 -> 31 (D -> AH)
                for (int day = 0; day < 31; day++)
                {
                    if (item.Ngay[day] > 0)
                    {
                        SetCellNumber(row, day + 3, Convert.ToDouble(item.Ngay[day]), decimalStyle);
                    }
                    else
                    {
                        SetCell(row, day + 3, string.Empty, borderStyle);
                    }
                }

                // AI
                SetCellNumber(row, 34, Convert.ToDouble(item.TongSoGioTangCa), decimalStyle);
                currentRow++;
            }
            #region Total Row

            var totalRow = data.FirstOrDefault(x => x.IsTotalRow);

            if (totalRow != null)
            {
                IRow row = sheet.GetRow(currentRow) ?? sheet.CreateRow(currentRow);
                SetCell(row, 0, "TỔNG CỘNG", totalStyle);
                SetCell(row, 1, "", totalStyle);
                SetCell(row, 2, "", totalStyle);

                sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(currentRow, currentRow, 0, 2));

                for (int day = 0; day < 31; day++)
                {
                    SetCellNumber(row, day + 3, Convert.ToDouble(totalRow.Ngay[day]), totalStyle);
                }

                SetCellNumber(row, 34, Convert.ToDouble(totalRow.TongSoGioTangCa), totalStyle);
            }
            #endregion

            ISheet yearSheet = workbook.GetSheetAt(1);
            #region Title

            if (!DateTime.TryParse(month + "-01", out selectedMonth))
            {
                selectedMonth = DateTime.Now;
            }

            yearSheet.GetRow(0).GetCell(0).SetCellValue($"BÁO CÁO THỐNG KÊ THEO NĂM {selectedMonth:yyyy}");
            #endregion

            var yearData = GetOvertimeYearData(month, dept, employee, currentUser);
            int currentYearRow = 2;

            foreach (var item in yearData.Where(x => !x.IsTotalRow))
            {
                IRow row = yearSheet.GetRow(currentYearRow) ?? yearSheet.CreateRow(currentYearRow);

                SetCell(row, 0, item.EmployeeCD, borderStyle);
                SetCell(row, 1, item.TenNhanVien, borderStyle);
                SetCell(row, 2, item.TenPhongBan, borderStyle);

                for (int m = 0; m < 12; m++)
                {
                    if (item.Thang[m] > 0)
                    {
                        SetCellNumber(row, m + 3, Convert.ToDouble(item.Thang[m]), decimalStyle);
                    }
                    else
                    {
                        SetCell(row, m + 3, "", borderStyle);
                    }
                }

                SetCellNumber(row, 15, Convert.ToDouble(item.TongCong), decimalStyle);
                currentYearRow++;
            }

            var totalYearRow = yearData.FirstOrDefault(x => x.IsTotalRow);

            if (totalYearRow != null)
            {
                IRow row = yearSheet.GetRow(currentYearRow) ?? yearSheet.CreateRow(currentYearRow);
                SetCell(row, 0, "TỔNG CỘNG", totalStyle);
                SetCell(row, 1, "", totalStyle);
                SetCell(row, 2, "", totalStyle);

                yearSheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(currentYearRow, currentYearRow, 0, 2));

                for (int i = 0; i < 12; i++)
                {
                    SetCellNumber(row, i + 3, Convert.ToDouble(totalYearRow.Thang[i]), totalStyle);
                }

                SetCellNumber(row, 15, Convert.ToDouble(totalYearRow.TongCong), totalStyle);
            }

            using (var ms = new MemoryStream())
            {
                workbook.Write(ms);
                string fileName = $"BaoCaoTangCa_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        private void SetCell(IRow row, int cellIndex, string value, ICellStyle style)
        {
            ICell cell = row.GetCell(cellIndex) ?? row.CreateCell(cellIndex);
            cell.SetCellValue(value);
            cell.CellStyle = style;
        }

        private void SetCellNumber(IRow row, int cellIndex, double value, ICellStyle style)
        {
            ICell cell = row.GetCell(cellIndex) ?? row.CreateCell(cellIndex);
            cell.SetCellValue(value);
            cell.CellStyle = style;
        }

        private List<AttendanceEmployeeItem> LoadAttendanceEmployees(string dept, string employee, UsersModel currentUser)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Dept", string.IsNullOrEmpty(dept) ? (object)DBNull.Value : dept),
                new SqlParameter("@Employee", string.IsNullOrEmpty(employee) ? (object)DBNull.Value : employee)
            };

            string permissionWhere = PermissionScopeHelper.BuildEmployeeScopeWhere(currentUser, _permissionService, dept,
                "nv", "nv.MaPhongBan", parameters);

            if (permissionWhere == "NO_ACCESS")
            {
                return new List<AttendanceEmployeeItem>();
            }

            string sql = @"SELECT nv.MaNhanVien, nv.TenNhanVien, nv.MaChamCong, pb.MaPhongBan, pb.TenPhongBan FROM NhanVien nv
            INNER JOIN PhongBan pb ON nv.MaPhongBan = pb.MaPhongBan
            WHERE (@Dept IS NULL OR pb.MaPhongBan = @Dept)
            AND (
                @Employee IS NULL
                OR nv.MaNhanVien LIKE '%' + @Employee + '%'
                OR nv.TenNhanVien LIKE N'%' + @Employee + N'%'
            ) ";
            sql += permissionWhere;
            sql += @" ORDER BY pb.TenPhongBan, nv.MaNhanVien";

            DataTable dt = SQLHelper.ExecuteDt(sql, parameters.ToArray());

            var list = new List<AttendanceEmployeeItem>();

            foreach (DataRow dr in dt.Rows)
            {
                list.Add(new AttendanceEmployeeItem
                {
                    EmployeeCD = dr["MaNhanVien"].ToString(),
                    TenNhanVien = dr["TenNhanVien"].ToString(),
                    MaChamCong = dr["MaChamCong"].ToString(),
                    MaPhongBan = dr["MaPhongBan"].ToString(),
                    TenPhongBan = dr["TenPhongBan"].ToString()
                });
            }
            return list;
        }

        private List<ReportOvertimeYearViewModel> GetOvertimeYearData(string month, string dept, string employee, UsersModel currentUser)
        {
            int yearValue;

            DateTime selectedDate;

            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out selectedDate))
            {
                yearValue = selectedDate.Year;
            }
            else
            {
                yearValue = DateTime.Now.Year;
            }

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Year", yearValue),
                new SqlParameter("@Dept", string.IsNullOrEmpty(dept) ? (object)DBNull.Value : dept),
                new SqlParameter("@Employee", string.IsNullOrEmpty(employee) ? (object)DBNull.Value : employee)
            };

            string permissionWhere = PermissionScopeHelper.BuildEmployeeScopeWhere(currentUser, _permissionService, dept,
                "nv", "nv.MaPhongBan", parameters);

            if (permissionWhere == "NO_ACCESS")
            {
                return new List<ReportOvertimeYearViewModel>();
            }

            string mySQL = @"
            WITH MonthlySum AS
            (
                SELECT d.EmployeeCD, MONTH(d.OvertimeDate) AS Thang, SUM(d.HoursWorked) AS TongThang
                FROM tbl_OvertimeHeaders h
                INNER JOIN tbl_Tickets t ON t.Id = h.TicketId
                INNER JOIN tbl_OvertimeDetails d ON h.Id = d.OvertimeHeaderId
                INNER JOIN NhanVien nv ON d.EmployeeCD = nv.MaNhanVien
                INNER JOIN PhongBan pb ON nv.MaPhongBan = pb.MaPhongBan
                WHERE YEAR(d.OvertimeDate) = @Year
                  AND t.StatusId = 2
                  AND (@Dept IS NULL OR pb.MaPhongBan = @Dept)
                  AND (@Employee IS NULL OR d.EmployeeCD = @Employee)
                  " + permissionWhere + @"
                GROUP BY d.EmployeeCD, MONTH(d.OvertimeDate)
            )
            SELECT p.EmployeeCD, nv.TenNhanVien, pb.TenPhongBan,
                p.[1] AS T1, p.[2] AS T2, p.[3] AS T3, p.[4] AS T4,
                p.[5] AS T5, p.[6] AS T6, p.[7] AS T7, p.[8] AS T8,
                p.[9] AS T9, p.[10] AS T10, p.[11] AS T11, p.[12] AS T12,
                ISNULL(p.[1],0) + ISNULL(p.[2],0) + ISNULL(p.[3],0) +
                ISNULL(p.[4],0) + ISNULL(p.[5],0) + ISNULL(p.[6],0) +
                ISNULL(p.[7],0) + ISNULL(p.[8],0) + ISNULL(p.[9],0) +
                ISNULL(p.[10],0) + ISNULL(p.[11],0) + ISNULL(p.[12],0)
                AS TongCong
            FROM
            (
                SELECT EmployeeCD, Thang, TongThang
                FROM MonthlySum
            ) src

            PIVOT
            (
                SUM(TongThang)
                FOR Thang IN
                (
                    [1],[2],[3],[4],[5],[6],
                    [7],[8],[9],[10],[11],[12]
                )
            ) p
            INNER JOIN NhanVien nv ON p.EmployeeCD = nv.MaNhanVien
            INNER JOIN PhongBan pb ON nv.MaPhongBan = pb.MaPhongBan
            ORDER BY pb.TenPhongBan, nv.MaNhanVien";

            DataTable dt = SQLHelper.ExecuteDt(mySQL, parameters.ToArray());

            var list = new List<ReportOvertimeYearViewModel>();

            foreach (DataRow dr in dt.Rows)
            {
                var row = new ReportOvertimeYearViewModel
                {
                    EmployeeCD = dr["EmployeeCD"].ToString(),
                    TenNhanVien = dr["TenNhanVien"].ToString(),
                    TenPhongBan = dr["TenPhongBan"].ToString(),
                    TongCong = dr["TongCong"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["TongCong"]),
                    IsTotalRow = false
                };

                for (int monthIndex = 1; monthIndex <= 12; monthIndex++)
                {
                    string col = $"T{monthIndex}";

                    if (dt.Columns.Contains(col) &&
                        dr[col] != DBNull.Value)
                    {
                        row.Thang[monthIndex - 1] =
                            Convert.ToDecimal(dr[col]);
                    }
                }

                list.Add(row);
            }

            var totalRow = new ReportOvertimeYearViewModel
            {
                EmployeeCD = "",
                TenNhanVien = "",
                TenPhongBan = "TỔNG CỘNG",
                IsTotalRow = true,
                TongCong = list.Sum(x => x.TongCong)
            };

            for (int i = 0; i < 12; i++)
            {
                totalRow.Thang[i] = list.Sum(x => x.Thang[i]);
            }

            list.Add(totalRow);

            return list;
        }

        private List<ReportOvertimeViewModel> GetOvertimeData(string month, string dept, string employee, UsersModel currentUser)
        {
            int monthValue;
            int yearValue;

            DateTime selectedDate;

            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out selectedDate))
            {
                monthValue = selectedDate.Month;
                yearValue = selectedDate.Year;
            }
            else
            {
                monthValue = DateTime.Now.Month;
                yearValue = DateTime.Now.Year;
            }

            var parameters = new List<SqlParameter>
    {
        new SqlParameter("@Month", monthValue),
        new SqlParameter("@Year", yearValue),
        new SqlParameter("@Dept", string.IsNullOrEmpty(dept) ? (object)DBNull.Value : dept),
        new SqlParameter("@Employee", string.IsNullOrEmpty(employee) ? (object)DBNull.Value : employee)
    };

            string permissionWhere = PermissionScopeHelper.BuildEmployeeScopeWhere(
                currentUser,
                _permissionService,
                dept,
                "nv",
                "nv.MaPhongBan",
                parameters
            );

            if (permissionWhere == "NO_ACCESS")
            {
                return new List<ReportOvertimeViewModel>();
            }

            string mySQL = @"
        WITH DailySum AS
        (
            SELECT 
                d.EmployeeCD,
                DAY(d.OvertimeDate) AS Ngay,
                SUM(d.HoursWorked) AS TongNgay
            FROM tbl_OvertimeHeaders h
            INNER JOIN tbl_Tickets t 
                ON t.Id = h.TicketId
            INNER JOIN tbl_OvertimeDetails d 
                ON h.Id = d.OvertimeHeaderId
            INNER JOIN NhanVien nv 
                ON d.EmployeeCD = nv.MaNhanVien
            INNER JOIN PhongBan pb 
                ON nv.MaPhongBan = pb.MaPhongBan
            WHERE MONTH(d.OvertimeDate) = @Month
              AND YEAR(d.OvertimeDate) = @Year
              AND t.StatusId = 2
              AND (@Dept IS NULL OR pb.MaPhongBan = @Dept)
              AND (@Employee IS NULL OR d.EmployeeCD = @Employee)
    ";

            mySQL += permissionWhere;

            mySQL += @"
            GROUP BY 
                d.EmployeeCD, 
                DAY(d.OvertimeDate)
        ),
        MonthlySum AS
        (
            SELECT 
                d.EmployeeCD,
                SUM(d.HoursWorked) AS TongSoGioTangCa
            FROM tbl_Tickets t
            INNER JOIN tbl_OvertimeHeaders h 
                ON t.Id = h.TicketId
            INNER JOIN tbl_OvertimeDetails d 
                ON h.Id = d.OvertimeHeaderId
            INNER JOIN NhanVien nv 
                ON d.EmployeeCD = nv.MaNhanVien
            INNER JOIN PhongBan pb 
                ON nv.MaPhongBan = pb.MaPhongBan
            WHERE MONTH(d.OvertimeDate) = @Month
              AND YEAR(d.OvertimeDate) = @Year
              AND t.StatusId = 2
              AND (@Dept IS NULL OR pb.MaPhongBan = @Dept)
              AND (@Employee IS NULL OR d.EmployeeCD = @Employee)
    ";

            mySQL += permissionWhere;

            mySQL += @"
            GROUP BY 
                d.EmployeeCD
        )
        SELECT 
            p.EmployeeCD,
            nv.TenNhanVien,
            nv.MaPhongBan,
            pb.TenPhongBan,
            p.[1] AS N1,
            p.[2] AS N2,
            p.[3] AS N3,
            p.[4] AS N4,
            p.[5] AS N5,
            p.[6] AS N6,
            p.[7] AS N7,
            p.[8] AS N8,
            p.[9] AS N9,
            p.[10] AS N10,
            p.[11] AS N11,
            p.[12] AS N12,
            p.[13] AS N13,
            p.[14] AS N14,
            p.[15] AS N15,
            p.[16] AS N16,
            p.[17] AS N17,
            p.[18] AS N18,
            p.[19] AS N19,
            p.[20] AS N20,
            p.[21] AS N21,
            p.[22] AS N22,
            p.[23] AS N23,
            p.[24] AS N24,
            p.[25] AS N25,
            p.[26] AS N26,
            p.[27] AS N27,
            p.[28] AS N28,
            p.[29] AS N29,
            p.[30] AS N30,
            p.[31] AS N31,
            m.TongSoGioTangCa
        FROM
        (
            SELECT 
                EmployeeCD, 
                Ngay, 
                TongNgay
            FROM DailySum
        ) src
        PIVOT
        (
            SUM(TongNgay)
            FOR Ngay IN
            (
                [1],[2],[3],[4],[5],[6],[7],[8],[9],[10],
                [11],[12],[13],[14],[15],[16],[17],[18],[19],[20],
                [21],[22],[23],[24],[25],[26],[27],[28],[29],[30],[31]
            )
        ) p
        INNER JOIN MonthlySum m 
            ON p.EmployeeCD = m.EmployeeCD
        INNER JOIN NhanVien nv 
            ON p.EmployeeCD = nv.MaNhanVien
        INNER JOIN PhongBan pb 
            ON nv.MaPhongBan = pb.MaPhongBan
        ORDER BY 
            pb.TenPhongBan,
            nv.MaNhanVien;
    ";

            DataTable dt = SQLHelper.ExecuteDt(mySQL, parameters.ToArray());

            var list = new List<ReportOvertimeViewModel>();

            foreach (DataRow dr in dt.Rows)
            {
                var row = new ReportOvertimeViewModel
                {
                    TenPhongBan = dr.Table.Columns.Contains("TenPhongBan") && dr["TenPhongBan"] != DBNull.Value
                        ? dr["TenPhongBan"].ToString()
                        : "",

                    TenNhanVien = dr.Table.Columns.Contains("TenNhanVien") && dr["TenNhanVien"] != DBNull.Value
                        ? dr["TenNhanVien"].ToString()
                        : "",

                    EmployeeCD = dr.Table.Columns.Contains("EmployeeCD") && dr["EmployeeCD"] != DBNull.Value
                        ? dr["EmployeeCD"].ToString()
                        : "",

                    TongSoGioTangCa = dr.Table.Columns.Contains("TongSoGioTangCa") && dr["TongSoGioTangCa"] != DBNull.Value
                        ? Convert.ToDecimal(dr["TongSoGioTangCa"])
                        : 0,

                    IsTotalRow = false
                };

                for (int d = 1; d <= 31; d++)
                {
                    string col = "N" + d;

                    if (dr.Table.Columns.Contains(col) && dr[col] != DBNull.Value)
                    {
                        row.Ngay[d - 1] = Convert.ToDecimal(dr[col]);
                    }
                }

                list.Add(row);
            }

            list = list
                .OrderBy(x => x.TenPhongBan)
                .ThenBy(x => x.EmployeeCD)
                .ToList();

            var totalRow = new ReportOvertimeViewModel
            {
                TenPhongBan = "Tổng cộng",
                TenNhanVien = null,
                EmployeeCD = null,
                IsTotalRow = true,
                TongSoGioTangCa = list.Sum(r => r.TongSoGioTangCa)
            };

            for (int d = 0; d < 31; d++)
            {
                totalRow.Ngay[d] = list.Sum(r => r.Ngay[d]);
            }

            list.Add(totalRow);

            return list;
        }

        public ActionResult Overtime(string month, string dept, string employee)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = Session["LoginInfo"] as UsersModel;

            var list = GetOvertimeData(month, dept, employee, currentUser);

            ViewBag.SelectedMonth = !string.IsNullOrEmpty(month) ? month : DateTime.Now.ToString("yyyy-MM");

            ViewBag.SelectedDept = dept;
            ViewBag.SelectedEmployee = employee;
            ViewBag.Departments = PermissionScopeHelper.LoadDepartmentsForUser(currentUser, _db, _permissionService);

            return View(list);
        }

        [HttpGet]
        public JsonResult OvertimeData(string month, string dept, string employee)
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

                var currentUser = Session["LoginInfo"] as UsersModel;

                var list = GetOvertimeData(month, dept, employee, currentUser);
                var employees = list.Where(x => !x.IsTotalRow).ToList();
                var totalEmployees = employees.Select(x => x.EmployeeCD).Distinct().Count();
                var totalSessions = employees.Sum(x => x.Ngay.Count(d => d > 0));
                var totalHours = employees.Sum(x => x.TongSoGioTangCa);
                var dayTotals = Enumerable.Range(0, 31).Select(d => employees.Sum(x => x.Ngay[d])).ToList();
                var maxDay = dayTotals.Any() && dayTotals.Max() > 0
                            ? dayTotals.IndexOf(dayTotals.Max()) + 1
                            : 0;
                var totalRow = list.FirstOrDefault(x => x.IsTotalRow);

                return Json(new
                {
                    success = true,
                    summary = new
                    {
                        totalEmployees,
                        totalSessions,
                        totalHours,
                        maxDay
                    },
                    rows = employees.Select(x => new
                    {
                        x.EmployeeCD,
                        x.TenNhanVien,
                        x.TenPhongBan,
                        Ngay = x.Ngay,
                        x.TongSoGioTangCa
                    }),
                    totalRow = totalRow == null ? null : new
                    {
                        totalRow.TenPhongBan,
                        Ngay = totalRow.Ngay,
                        totalRow.TongSoGioTangCa
                    }
                },
                JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                },
                JsonRequestBehavior.AllowGet);
            }
        }

    }
}