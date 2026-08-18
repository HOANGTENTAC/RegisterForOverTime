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
    public class HomeController : Controller
    {
        private readonly PermissionService _permissionService;
        public HomeController()
        {
            _permissionService = new PermissionService();
        }

        public ActionResult Index(int? month, int? year)
        {
            var user = CurrentUser;

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(BuildDashboard(user, month ?? DateTime.Now.Month, year ?? DateTime.Now.Year));
        }

        private UsersModel CurrentUser
        {
            get
            {
                return Session["LoginInfo"] as UsersModel;
            }
        }

        private HomePageViewModel BuildDashboard(UsersModel user, int month, int year)
        {
            DateTime fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            DateTime toDate = DateTime.Now;

            return new HomePageViewModel
            {
                User = user,
                CurrentMonth = month,
                CurrentYear = year,
                FromDate = fromDate,
                ToDate = toDate,
                WorkingDays = LoadCalculateWork(user),
                TotalHours = LoadCalculateOvertime(user),
                Calendar = LoadCalendar(month, year),
                RecentRequests = LoadRecentRequests(user, fromDate, toDate)
            };
        }

        private int LoadCalculateWork(UsersModel user)
        {
            string sql = @"SELECT COUNT(DISTINCT NgayCham) Total FROM [MITACOSQL].[dbo].[CheckInOut]
            WHERE MaChamCong = @MaChamCong AND MONTH(NgayCham) = @Month AND YEAR(NgayCham) = @Year";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@MaChamCong", user.MaChamCong),
                new SqlParameter("@Month", DateTime.Now.Month),
                new SqlParameter("@Year", DateTime.Now.Year));

            return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["Total"]) : 0;
        }

        private List<CalendarDayModel> LoadCalendar(int month, int year)
        {
            // Lấy dữ liệu ngày nghỉ
            string sqlNghi = @"SELECT Nam,Thang,D1,D2,D3,D4,D5,D6,D7,D8,D9,D10,D11,D12,D13,D14,D15,D16,D17,D18,D19,D20,D21,
                D22,D23,D24,D25,D26,D27,D28,D29,D30,D31, b.Day, b.Note FROM [MITACOSQL].[dbo].[NgayNghi] a
                LEFT JOIN tbl_Holiday b on a.Nam = b.Year and a.Thang = b.Month
                WHERE Nam=@Year AND Thang=@Month";
            DataTable dtCalendar = SQLHelper.ExecuteDt(sqlNghi,
                new SqlParameter("@Year", year),
                new SqlParameter("@Month", month));
            List<CalendarDayModel> calendar = new List<CalendarDayModel>();
            int days = DateTime.DaysInMonth(year, month);
            for (int i = 1; i <= days; i++)
            {
                bool isOff = false;
                bool isHolyday = false;
                string note = string.Empty;
                if (dtCalendar.Rows.Count > 0)
                {
                    string column = $"D{i}";
                    isOff = Convert.ToBoolean(dtCalendar.Rows[0][column]);
                    foreach (DataRow row in dtCalendar.Rows)
                    {
                        if (row["Day"].ToString() == i.ToString())
                        {
                            isHolyday = true;
                            break;
                        }
                    }
                }
                DateTime date = new DateTime(year, month, i);
                if (isOff == true && isHolyday == false)
                {
                    note = "Ngày nghỉ";
                }
                else if (isOff == true && isHolyday == true)
                {
                    note = dtCalendar.Rows[0]["Note"].ToString();
                }
                else
                {
                    note = "Ngày làm việc";
                }
                calendar.Add(new CalendarDayModel
                {
                    DayNumber = i,
                    IsOff = isOff,
                    IsToday = date.Date == DateTime.Today,
                    IsHoliday = isHolyday,
                    Note = note,
                    DayOfWeek = date.DayOfWeek
                });
            }
            return calendar;
        }

        private List<TblTicketsModel> LoadRecentRequests(UsersModel user, DateTime fromDate, DateTime toDate)
        {
            if (user == null)
            {
                return new List<TblTicketsModel>();
            }

            bool canViewAll = _permissionService.CanViewAllData(user);

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@FromDate", fromDate.Date),
                new SqlParameter("@ToDate", toDate.Date)
            };

            string userCondition = "";

            if (!canViewAll)
            {
                userCondition = " AND (CreatedUserCD = @CreatedUserCD OR ConfirmUserCD = @CreatedUserCD) ";
                parameters.Add(new SqlParameter("@CreatedUserCD", user.MaNhanVien));
            }

            string sqlTickets = @"SELECT * FROM
            (
                SELECT  a.Id, a.TicketTypeId, a.TicketNo, b.StatusName, c.TypeName, h.RequestDate,
                    a.StatusId, h.Reason AS ReasonRequest, a.Reason, a.CreatedUserCD, h.ConfirmUserCD, a.CreatedDate
                FROM tbl_Tickets a
                INNER JOIN mst_TicketStatus b ON a.StatusId = b.StatusId
                INNER JOIN mst_TicketTypes c ON a.TicketTypeId = c.TicketTypeId
                INNER JOIN tbl_OvertimeHeaders h ON a.Id = h.TicketId
                WHERE h.RequestDate BETWEEN @FromDate AND @ToDate

                UNION ALL

                SELECT a.Id, a.TicketTypeId, a.TicketNo, b.StatusName, c.TypeName, h.RequestDate, a.StatusId,
                    h.Reason AS ReasonRequest, a.Reason, a.CreatedUserCD, h.ConfirmUserCD, a.CreatedDate
                FROM tbl_Tickets a
                INNER JOIN mst_TicketStatus b ON a.StatusId = b.StatusId
                INNER JOIN mst_TicketTypes c ON a.TicketTypeId = c.TicketTypeId
                INNER JOIN tbl_ShiftRegisterHeaders h ON a.Id = h.TicketId
                WHERE h.RequestDate BETWEEN @FromDate AND @ToDate
            ) X
            WHERE 1 = 1
            ";

            sqlTickets += userCondition;

            sqlTickets += @" ORDER BY CreatedDate DESC";

            DataTable dtTickets = SQLHelper.ExecuteDt(sqlTickets, parameters.ToArray());

            List<TblTicketsModel> recentRequests = new List<TblTicketsModel>();

            foreach (DataRow row in dtTickets.Rows)
            {
                recentRequests.Add(new TblTicketsModel
                {
                    Id = Convert.ToInt32(row["Id"]),
                    TicketNo = row["TicketNo"].ToString(),
                    TicketTypeId = Convert.ToInt32(row["TicketTypeId"]),
                    TypeName = row["TypeName"].ToString(),
                    StatusName = row["StatusName"].ToString(),
                    StatusId = Convert.ToInt32(row["StatusId"]),
                    RequestDate = Convert.ToDateTime(row["RequestDate"]),
                    ReasonRequest = row["ReasonRequest"] == DBNull.Value ? "" : row["ReasonRequest"].ToString(),
                    Reason = row["Reason"] == DBNull.Value ? "" : row["Reason"].ToString()
                });
            }

            return recentRequests;
        }

        private decimal LoadCalculateOvertime(UsersModel user)
        {
            string sql = @"SELECT SUM(d.HoursWorked) AS MonthlyHours FROM tbl_OvertimeHeaders h
            INNER JOIN tbl_OvertimeDetails d ON h.Id = d.OvertimeHeaderId
            INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
            WHERE t.StatusId = 2 AND MONTH(d.OvertimeDate)=MONTH(GETDATE())
                AND YEAR(d.OvertimeDate)=YEAR(GETDATE()) AND d.EmployeeCD=@EmployeeCD";

            DataTable dt = SQLHelper.ExecuteDt(sql, new SqlParameter("@EmployeeCD", user.MaNhanVien));

            if (dt.Rows.Count == 0 || dt.Rows[0]["MonthlyHours"] == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToDecimal(dt.Rows[0]["MonthlyHours"]);
        }

        [HttpGet]
        public JsonResult SearchRequests(string keyword, DateTime? fromDate, DateTime? toDate)
        {
            var user = CurrentUser;

            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Phiên đăng nhập đã hết hạn."
                }, JsonRequestBehavior.AllowGet);
            }

            DateTime from = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime to = toDate ?? DateTime.Now;

            var requests = LoadRecentRequests(user, from, to);

            if (!string.IsNullOrEmpty(keyword))
            {
                requests = requests
                    .Where(r =>
                        (!string.IsNullOrEmpty(r.TicketNo) && r.TicketNo.Contains(keyword)) ||
                        (!string.IsNullOrEmpty(r.ReasonRequest) && r.ReasonRequest.Contains(keyword)))
                    .ToList();
            }

            return Json(new
            {
                success = true,
                data = requests.Select(r => new
                {
                    r.Id,
                    r.TicketNo,
                    r.TicketTypeId,
                    r.TypeName,
                    r.StatusId,
                    r.StatusName,
                    RequestDate = r.RequestDate.ToString("dd/MM/yyyy"),
                    r.ReasonRequest
                })
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult DashboardSummary()
        {
            var user = CurrentUser;

            if (user == null)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            return Json(
                new
                {
                    success = true,

                    workingDays = LoadCalculateWork(user),

                    overtime = LoadCalculateOvertime(user)
                },
                JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetCalendar(int month, int year)
        {
            List<CalendarDayModel> calendar = LoadCalendar(month, year);

            return Json(new
            {
                success = true,
                month,
                year,
                data = calendar.Select(x => new
                {
                    x.DayNumber,
                    x.IsOff,
                    x.IsHoliday,
                    x.IsToday
                })
            },
            JsonRequestBehavior.AllowGet);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}