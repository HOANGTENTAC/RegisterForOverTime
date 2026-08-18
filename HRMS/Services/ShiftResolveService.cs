using HRMS.Common;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.ViewModels;
using System;
using System.Data;
using System.Data.SqlClient;

namespace HRMS.Services
{
    public class ShiftResolveService
    {
        private readonly ApplicationDbContext _context;

        public ShiftResolveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public EffectiveShiftViewModel GetEffectiveShift(string employeeCd, DateTime workDate)
        {
            if (string.IsNullOrEmpty(employeeCd))
            {
                return null;
            }

            var registeredShift = GetRegisteredShift(employeeCd, workDate);

            if (registeredShift != null)
            {
                return registeredShift;
            }

            var defaultShift = GetDefaultDepartmentShift(employeeCd, workDate);

            if (defaultShift != null)
            {
                return defaultShift;
            }

            return null;
        }

        private EffectiveShiftViewModel GetRegisteredShift(string employeeCd, DateTime workDate)
        {
            string sql = @"SELECT TOP 1 st.ShiftTypeId, st.ShiftCode, st.ShiftName, st.StartTime,
                    st.EndTime, st.BreakMinutes, st.IsNightShift FROM tbl_ShiftRegisterDetails d
                INNER JOIN tbl_ShiftRegisterHeaders h ON d.ShiftRegisterHeaderId = h.Id
                INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
                INNER JOIN mst_ShiftTypes st ON d.ShiftTypeId = st.ShiftTypeId
                WHERE d.EmployeeCD = @EmployeeCD AND CAST(d.WorkDate AS date) = @WorkDate
                  AND t.TicketTypeId = @TicketTypeId AND t.StatusId = @AcceptedStatus
                ORDER BY d.Id DESC";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@EmployeeCD", employeeCd),
                new SqlParameter("@WorkDate", workDate.Date),
                new SqlParameter("@TicketTypeId", (int)Enums.RequestTypeEnum.SR),
                new SqlParameter("@AcceptedStatus", (int)Enums.RequestStatusEnum.Finished));

            if (dt.Rows.Count == 0)
            {
                return null;
            }

            return MapEffectiveShift(dt.Rows[0], "REGISTERED");
        }

        private EffectiveShiftViewModel GetDefaultDepartmentShift(string employeeCd, DateTime workDate)
        {
            string sql = @"SELECT TOP 1 st.ShiftTypeId, st.ShiftCode, st.ShiftName, st.StartTime,
                    st.EndTime, st.BreakMinutes, st.IsNightShift
                FROM [MITACOSQL].[dbo].[NHANVIEN] nv
                INNER JOIN [TIME_KEEPING].[dbo].[mst_DefaultShifts] ds ON nv.MaPhongBan = ds.DepartmentCD
                INNER JOIN [TIME_KEEPING].[dbo].[mst_ShiftTypes] st ON ds.ShiftTypeId = st.ShiftTypeId
                WHERE nv.MaNhanVien = @EmployeeCD AND ds.IsActive = 1 AND ds.EffectiveFrom <= @WorkDate
                  AND (
                        ds.EffectiveTo IS NULL
                        OR ds.EffectiveTo >= @WorkDate
                      )
                ORDER BY ds.EffectiveFrom DESC, ds.Id DESC";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@EmployeeCD", employeeCd),
                new SqlParameter("@WorkDate", workDate.Date));

            if (dt.Rows.Count == 0)
            {
                return null;
            }

            return MapEffectiveShift(dt.Rows[0], "DEFAULT_DEPARTMENT");
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
    }
}