using HRMS.Common;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.Utils;
using HRMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace HRMS.Services
{
    public class ShiftRegisterService
    {
        private readonly ApplicationDbContext _context;

        public ShiftRegisterService(ApplicationDbContext context)
        {
            _context = context;
        }

        public ServiceResult CreateShiftRegisterRequest(ShiftRegisterRequestModel request)
        {
            if (request == null)
            {
                return Fail("Dữ liệu đăng ký ca không hợp lệ.");
            }

            if (request.EmployeeCDs == null ||
                !request.EmployeeCDs.Any(x => !string.IsNullOrEmpty(x.MaNhanVien)))
            {
                return Fail("Bạn phải chọn ít nhất 1 nhân viên.");
            }

            if (request.FromDate.Date > request.ToDate.Date)
            {
                return Fail("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            }

            if (request.ShiftTypeId <= 0)
            {
                return Fail("Vui lòng chọn ca làm việc.");
            }

            var selectedEmployees = request.EmployeeCDs
                .Where(x => !string.IsNullOrEmpty(x.MaNhanVien))
                .Select(x => x.MaNhanVien)
                .Distinct()
                .ToList();

            var conflicts = LoadShiftRegisterConflicts(selectedEmployees, request.FromDate.Date, request.ToDate.Date);

            if (conflicts.Count > 0)
            {
                return Fail(BuildShiftRegisterConflictMessage(conflicts));
            }

            var attendanceValidation = ValidateAttendanceMatchesShift(selectedEmployees, request.FromDate.Date,
                request.ToDate.Date, request.ShiftTypeId);

            if (!attendanceValidation.Success)
            {
                return attendanceValidation;
            }

            int statusId = (int)Enums.RequestStatusEnum.Pending;

            if (request.CreateAsFinished)
            {
                statusId = (int)Enums.RequestStatusEnum.Finished;
            }
            else if (request.CreateAsManagerAccepted)
            {
                statusId = (int)Enums.RequestStatusEnum.ManagerAccepted;
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var ticket = new TblTicketsModel
                    {
                        TicketNo = GenerateHelper.GenerateRequestNo((int)Enums.RequestTypeEnum.SR),
                        TicketTypeId = (int)Enums.RequestTypeEnum.SR,
                        StatusId = statusId,
                        CreatedUserCD = request.CreatedUserCD,
                        CreatedDate = DateTime.Now,
                        UpdateUserCD = request.CreatedUserCD,
                        UpdateDate = DateTime.Now
                    };

                    _context.TblTickets.Add(ticket);
                    _context.SaveChanges();

                    request.TicketId = ticket.Id;

                    var header = new TblShiftRegisterHeadersModel
                    {
                        TicketId = ticket.Id,
                        RequestDate = DateTime.Today,
                        FromDate = request.FromDate.Date,
                        ToDate = request.ToDate.Date,
                        ConfirmUserCD = request.ConfirmUserCD,
                        Reason = request.Reason,
                        CreatedDate = DateTime.Now
                    };

                    _context.TblShiftRegisterHeaders.Add(header);
                    _context.SaveChanges();

                    request.HeaderId = header.Id;

                    foreach (var employeeCd in selectedEmployees)
                    {
                        for (var date = request.FromDate.Date; date <= request.ToDate.Date; date = date.AddDays(1))
                        {
                            var detail = new TblShiftRegisterDetailsModel
                            {
                                ShiftRegisterHeaderId = header.Id,
                                EmployeeCD = employeeCd,
                                WorkDate = date,
                                ShiftTypeId = request.ShiftTypeId,
                                CreatedDate = DateTime.Now
                            };

                            _context.TblShiftRegisterDetails.Add(detail);
                        }
                    }

                    _context.SaveChanges();
                    tran.Commit();

                    if (statusId == (int)Enums.RequestStatusEnum.Finished)
                    {
                        return Ok("Đăng ký ca thành công và đã hoàn tất.");
                    }

                    if (statusId == (int)Enums.RequestStatusEnum.ManagerAccepted)
                    {
                        return Ok("Đăng ký ca thành công, đã qua bước quản lý duyệt.");
                    }

                    return Ok("Đăng ký ca thành công, đang chờ quản lý duyệt.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        public ServiceResult ManagerAcceptShiftRegisterRequest(int ticketId, string managerUserCD, bool canForce = false)
        {
            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var ticket = _context.TblTickets.FirstOrDefault(t => t.Id == ticketId);
                    var header = _context.TblShiftRegisterHeaders.FirstOrDefault(h => h.TicketId == ticketId);

                    if (ticket == null || header == null)
                    {
                        return Fail("Không tìm thấy phiếu đăng ký ca.");
                    }

                    if (!canForce && ticket.StatusId != (int)Enums.RequestStatusEnum.Pending)
                    {
                        return Fail("Phiếu không còn ở trạng thái chờ quản lý duyệt.");
                    }

                    if (!canForce && header.ConfirmUserCD != managerUserCD)
                    {
                        return Fail("Bạn không có quyền duyệt phiếu này.");
                    }

                    ticket.StatusId = (int)Enums.RequestStatusEnum.ManagerAccepted;
                    ticket.UpdateUserCD = managerUserCD;
                    ticket.UpdateDate = DateTime.Now;

                    header.UpdatedDate = DateTime.Now;

                    _context.SaveChanges();
                    tran.Commit();

                    return Ok("Quản lý đã duyệt phiếu đăng ký ca.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        public ServiceResult ManagerRejectShiftRegisterRequest(int ticketId, string managerUserCD, string reason, bool canForce = false)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Fail("Vui lòng nhập lý do từ chối.");
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var ticket = _context.TblTickets.FirstOrDefault(t => t.Id == ticketId);
                    var header = _context.TblShiftRegisterHeaders.FirstOrDefault(h => h.TicketId == ticketId);

                    if (ticket == null || header == null)
                    {
                        return Fail("Không tìm thấy phiếu đăng ký ca.");
                    }

                    if (!canForce && ticket.StatusId != (int)Enums.RequestStatusEnum.Pending)
                    {
                        return Fail("Phiếu không còn ở trạng thái chờ quản lý duyệt.");
                    }

                    if (!canForce && header.ConfirmUserCD != managerUserCD)
                    {
                        return Fail("Bạn không có quyền từ chối phiếu này.");
                    }

                    ticket.StatusId = (int)Enums.RequestStatusEnum.ManagerRejected;
                    ticket.Reason = reason;
                    ticket.UpdateUserCD = managerUserCD;
                    ticket.UpdateDate = DateTime.Now;

                    header.UpdatedDate = DateTime.Now;

                    _context.SaveChanges();
                    tran.Commit();

                    return Ok("Quản lý đã từ chối phiếu đăng ký ca.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        public ServiceResult HrFinishShiftRegisterRequest(int ticketId, string hrUserCD, bool canHrProcess)
        {
            if (!canHrProcess)
            {
                return Fail("Bạn không có quyền hoàn tất phiếu.");
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var ticket = _context.TblTickets.FirstOrDefault(t => t.Id == ticketId);
                    var header = _context.TblShiftRegisterHeaders.FirstOrDefault(h => h.TicketId == ticketId);

                    if (ticket == null || header == null)
                    {
                        return Fail("Không tìm thấy phiếu đăng ký ca.");
                    }

                    if (ticket.StatusId != (int)Enums.RequestStatusEnum.ManagerAccepted)
                    {
                        return Fail("Chỉ phiếu đã được quản lý duyệt mới có thể hoàn tất.");
                    }

                    ticket.StatusId = (int)Enums.RequestStatusEnum.Finished;
                    ticket.UpdateUserCD = hrUserCD;
                    ticket.UpdateDate = DateTime.Now;

                    header.UpdatedDate = DateTime.Now;

                    _context.SaveChanges();
                    tran.Commit();

                    return Ok("Phiếu đăng ký ca đã hoàn tất.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        public ServiceResult HrRejectShiftRegisterRequest(int ticketId, string hrUserCD, string reason, bool canHrProcess)
        {
            if (!canHrProcess)
            {
                return Fail("Bạn không có quyền từ chối phiếu ở bước nhân sự.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return Fail("Vui lòng nhập lý do từ chối.");
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var ticket = _context.TblTickets.FirstOrDefault(t => t.Id == ticketId);
                    var header = _context.TblShiftRegisterHeaders.FirstOrDefault(h => h.TicketId == ticketId);

                    if (ticket == null || header == null)
                    {
                        return Fail("Không tìm thấy phiếu đăng ký ca.");
                    }

                    if (ticket.StatusId != (int)Enums.RequestStatusEnum.ManagerAccepted)
                    {
                        return Fail("Chỉ phiếu đã được quản lý duyệt mới có thể bị nhân sự từ chối.");
                    }

                    ticket.StatusId = (int)Enums.RequestStatusEnum.HrRejected;
                    ticket.Reason = reason;
                    ticket.UpdateUserCD = hrUserCD;
                    ticket.UpdateDate = DateTime.Now;

                    header.UpdatedDate = DateTime.Now;

                    _context.SaveChanges();
                    tran.Commit();

                    return Ok("Nhân sự đã từ chối phiếu đăng ký ca.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        public ServiceResult CancelShiftRegisterRequest(int ticketId, string userCD, bool canForceCancel)
        {
            if (!canForceCancel)
            {
                return Fail("Bạn không có quyền hủy phiếu này.");
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var ticket = _context.TblTickets.FirstOrDefault(t =>
                        t.Id == ticketId &&
                        t.TicketTypeId == (int)Enums.RequestTypeEnum.SR);

                    if (ticket == null)
                    {
                        return Fail("Không tìm thấy phiếu đăng ký ca.");
                    }

                    ticket.StatusId = (int)Enums.RequestStatusEnum.Cancelled;
                    ticket.UpdateUserCD = userCD;
                    ticket.UpdateDate = DateTime.Now;

                    _context.SaveChanges();
                    tran.Commit();

                    return Ok("Đã hủy phiếu đăng ký ca.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        public ServiceResult UpdateShiftRegisterRequest(ShiftRegisterRequestModel request, bool canForceUpdate = false)
        {
            if (request == null)
            {
                return Fail("Dữ liệu đăng ký ca không hợp lệ.");
            }

            if (request.TicketId <= 0 || request.HeaderId <= 0)
            {
                return Fail("Thông tin phiếu không hợp lệ.");
            }

            if (request.EmployeeCDs == null || !request.EmployeeCDs.Any(x => !string.IsNullOrEmpty(x.MaNhanVien)))
            {
                return Fail("Bạn phải chọn ít nhất 1 nhân viên.");
            }

            if (request.FromDate.Date > request.ToDate.Date)
            {
                return Fail("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            }

            if (request.ShiftTypeId <= 0)
            {
                return Fail("Vui lòng chọn ca làm việc.");
            }

            var selectedEmployees = request.EmployeeCDs
               .Where(x => !string.IsNullOrEmpty(x.MaNhanVien)).Select(x => x.MaNhanVien).Distinct().ToList();

            var attendanceValidation = ValidateAttendanceMatchesShift(selectedEmployees, request.FromDate.Date,
                request.ToDate.Date, request.ShiftTypeId);

            if (!attendanceValidation.Success)
            {
                return attendanceValidation;
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var ticket = _context.TblTickets.FirstOrDefault(x => x.Id == request.TicketId);

                    if (ticket == null)
                    {
                        return Fail("Không tìm thấy phiếu đăng ký ca.");
                    }

                    if (!canForceUpdate && ticket.StatusId != (int)Enums.RequestStatusEnum.Pending)
                    {
                        return Fail("Phiếu đã được xử lý, bạn không thể chỉnh sửa.");
                    }

                    var header = _context.TblShiftRegisterHeaders.FirstOrDefault(x =>
                        x.Id == request.HeaderId &&
                        x.TicketId == request.TicketId);

                    if (header == null)
                    {
                        return Fail("Không tìm thấy thông tin đăng ký ca.");
                    }

                    int statusId = (int)Enums.RequestStatusEnum.Pending;

                    if (request.CreateAsFinished)
                    {
                        statusId = (int)Enums.RequestStatusEnum.Finished;
                    }
                    else if (request.CreateAsManagerAccepted)
                    {
                        statusId = (int)Enums.RequestStatusEnum.ManagerAccepted;
                    }

                    ticket.StatusId = statusId;

                    ticket.UpdateUserCD = request.CreatedUserCD;
                    ticket.UpdateDate = DateTime.Now;
                    ticket.Reason = null;

                    header.FromDate = request.FromDate.Date;
                    header.ToDate = request.ToDate.Date;
                    header.ConfirmUserCD = request.ConfirmUserCD;
                    header.Reason = StringExtensions.Nz(request.Reason);
                    header.UpdatedDate = DateTime.Now;

                    var oldDetails = _context.TblShiftRegisterDetails
                        .Where(x => x.ShiftRegisterHeaderId == header.Id)
                        .ToList();

                    _context.TblShiftRegisterDetails.RemoveRange(oldDetails);
                    _context.SaveChanges();

                    foreach (var employeeCd in selectedEmployees)
                    {
                        for (var date = request.FromDate.Date; date <= request.ToDate.Date; date = date.AddDays(1))
                        {
                            var conflicts = LoadShiftRegisterConflicts(selectedEmployees, request.FromDate.Date,
                                request.ToDate.Date, request.HeaderId);

                            if (conflicts.Count > 0)
                            {
                                return Fail(BuildShiftRegisterConflictMessage(conflicts));
                            }

                            var detail = new TblShiftRegisterDetailsModel
                            {
                                ShiftRegisterHeaderId = header.Id,
                                EmployeeCD = employeeCd,
                                WorkDate = date,
                                ShiftTypeId = request.ShiftTypeId,
                                CreatedDate = DateTime.Now
                            };

                            _context.TblShiftRegisterDetails.Add(detail);
                        }
                    }

                    _context.SaveChanges();
                    tran.Commit();

                    if (statusId == (int)Enums.RequestStatusEnum.Finished)
                    {
                        return Ok("Đã cập nhật phiếu đăng ký ca và hoàn tất.");
                    }

                    if (statusId == (int)Enums.RequestStatusEnum.ManagerAccepted)
                    {
                        return Ok("Đã cập nhật phiếu đăng ký ca và đã qua bước quản lý duyệt.");
                    }

                    return Ok("Đã cập nhật phiếu đăng ký ca.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        private ServiceResult ValidateAttendanceMatchesShift(List<string> employeeCodes, DateTime fromDate, DateTime toDate, int requestedShiftTypeId)
        {
            if (employeeCodes == null || employeeCodes.Count == 0)
            {
                return Ok("");
            }

            var requestedShift = _context.MstShiftTypes.FirstOrDefault(x => x.ShiftTypeId == requestedShiftTypeId);

            if (requestedShift == null)
            {
                return Fail("Không tìm thấy ca làm việc đã chọn.");
            }

            var shiftTypes = _context.MstShiftTypes
                .OrderBy(x => x.ShiftTypeId)
                .ToList();

            var attendances = LoadAttendanceByEmployees(
                employeeCodes,
                fromDate,
                toDate
            );

            var mismatches = new List<AttendanceShiftMismatchItem>();

            foreach (var item in attendances)
            {
                if (!item.FirstCheckIn.HasValue)
                {
                    continue;
                }

                var suggestedShift = DetectShiftByCheckIn(
                    item.FirstCheckIn.Value,
                    shiftTypes
                );

                if (suggestedShift == null)
                {
                    mismatches.Add(new AttendanceShiftMismatchItem
                    {
                        EmployeeCD = item.EmployeeCD,
                        EmployeeName = item.EmployeeName,
                        WorkDate = item.WorkDate,
                        FirstCheckIn = item.FirstCheckIn,
                        LastCheckOut = item.LastCheckOut,
                        RequestedShiftName = requestedShift.ShiftName,
                        SuggestedShiftName = "Không xác định"
                    });

                    continue;
                }

                if (suggestedShift.ShiftTypeId != requestedShiftTypeId)
                {
                    mismatches.Add(new AttendanceShiftMismatchItem
                    {
                        EmployeeCD = item.EmployeeCD,
                        EmployeeName = item.EmployeeName,
                        WorkDate = item.WorkDate,
                        FirstCheckIn = item.FirstCheckIn,
                        LastCheckOut = item.LastCheckOut,
                        RequestedShiftName = requestedShift.ShiftName,
                        SuggestedShiftName = suggestedShift.ShiftName
                    });
                }
            }

            if (mismatches.Count > 0)
            {
                return Fail(BuildAttendanceShiftMismatchMessage(mismatches));
            }

            return Ok("");
        }

        private List<AttendancePunchItem> LoadAttendanceByEmployees(List<string> employeeCodes, DateTime fromDate, DateTime toDate)
        {
            if (employeeCodes == null || employeeCodes.Count == 0)
            {
                return new List<AttendancePunchItem>();
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

            string sql = $@"
            SELECT nv.MaNhanVien, nv.TenNhanVien,
                CAST(c.NgayCham AS date) AS WorkDate,
                MIN(c.GioCham) AS FirstCheckIn,
                MAX(c.GioCham) AS LastCheckOut
            FROM [MITACOSQL].[dbo].[NHANVIEN] nv
            INNER JOIN [MITACOSQL].[dbo].[CheckInOut] c ON nv.MaChamCong = c.MaChamCong
            WHERE nv.MaNhanVien IN ({string.Join(",", names)})
              AND CAST(c.NgayCham AS date) BETWEEN @FromDate AND @ToDate
            GROUP BY nv.MaNhanVien, nv.TenNhanVien, CAST(c.NgayCham AS date)
            ORDER BY nv.MaNhanVien, CAST(c.NgayCham AS date)";

            DataTable dt = SQLHelper.ExecuteDt(sql, parameters.ToArray());

            var result = new List<AttendancePunchItem>();

            foreach (DataRow row in dt.Rows)
            {
                DateTime? first = row["FirstCheckIn"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(row["FirstCheckIn"]);

                DateTime? last = row["LastCheckOut"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(row["LastCheckOut"]);

                result.Add(new AttendancePunchItem
                {
                    EmployeeCD = row["MaNhanVien"].ToString(),
                    EmployeeName = row["TenNhanVien"].ToString(),
                    WorkDate = Convert.ToDateTime(row["WorkDate"]),
                    FirstCheckIn = first.HasValue ? first.Value.TimeOfDay : (TimeSpan?)null,
                    LastCheckOut = last.HasValue ? last.Value.TimeOfDay : (TimeSpan?)null
                });
            }

            return result;
        }

        private string BuildAttendanceShiftMismatchMessage(List<AttendanceShiftMismatchItem> mismatches)
        {
            // Kiểm tra danh sách rỗng hoặc null để tránh lỗi crash
            if (mismatches == null || mismatches.Count == 0)
            {
                return string.Empty;
            }

            var displayItems = mismatches
                .Take(5)
                .Select(x => $"{x.EmployeeCD} - {x.EmployeeName} ngày {x.WorkDate:dd/MM/yyyy}: " +
                             $"chấm công {FormatTime(x.FirstCheckIn)} - {FormatTime(x.LastCheckOut)}, " +
                             $"phù hợp {x.SuggestedShiftName}, không phải {x.RequestedShiftName}")
                .ToList();

            // Sử dụng Environment.NewLine để tự động xuống hàng rõ ràng cho từng nhân viên
            string finalResult = string.Join(Environment.NewLine, displayItems);

            string message = "Ca đăng ký không phù hợp với dữ liệu chấm công: "
                + Environment.NewLine
                + finalResult;

            if (mismatches.Count > 5)
            {
                message += Environment.NewLine + $"và {mismatches.Count - 5} dòng khác.";
            }

            return message;
        }

        private List<ShiftRegisterConflictItem> LoadShiftRegisterConflicts(List<string> employeeCodes, DateTime fromDate,
            DateTime toDate, int? ignoreHeaderId = null)
        {
            if (employeeCodes == null || employeeCodes.Count == 0)
            {
                return new List<ShiftRegisterConflictItem>();
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
            parameters.Add(new SqlParameter("@RequestType", (int)Enums.RequestTypeEnum.SR));
            parameters.Add(new SqlParameter("@Pending", (int)Enums.RequestStatusEnum.Pending));
            parameters.Add(new SqlParameter("@ManagerAccepted", (int)Enums.RequestStatusEnum.ManagerAccepted));
            parameters.Add(new SqlParameter("@Finished", (int)Enums.RequestStatusEnum.Finished));
            parameters.Add(new SqlParameter("@IgnoreHeaderId", ignoreHeaderId.HasValue ? (object)ignoreHeaderId.Value : DBNull.Value));

            string sql = $@"SELECT DISTINCT d.EmployeeCD, nv.TenNhanVien, d.WorkDate, t.TicketNo, s.StatusName
            FROM tbl_ShiftRegisterDetails d
            INNER JOIN tbl_ShiftRegisterHeaders h ON d.ShiftRegisterHeaderId = h.Id
            INNER JOIN tbl_Tickets t ON h.TicketId = t.Id
            INNER JOIN mst_TicketStatus s ON t.StatusId = s.StatusId
            INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] nv ON d.EmployeeCD = nv.MaNhanVien
            WHERE d.EmployeeCD IN ({string.Join(",", names)})
              AND CAST(d.WorkDate AS date) BETWEEN @FromDate AND @ToDate
              AND t.TicketTypeId = @RequestType
              AND t.StatusId IN (@Pending, @ManagerAccepted, @Finished)
              AND (
                    @IgnoreHeaderId IS NULL
                    OR h.Id <> @IgnoreHeaderId
                  )
            ORDER BY d.EmployeeCD, d.WorkDate";

            DataTable dt = SQLHelper.ExecuteDt(sql, parameters.ToArray());

            var result = new List<ShiftRegisterConflictItem>();

            foreach (DataRow row in dt.Rows)
            {
                result.Add(new ShiftRegisterConflictItem
                {
                    EmployeeCD = row["EmployeeCD"].ToString(),
                    EmployeeName = row["TenNhanVien"].ToString(),
                    WorkDate = Convert.ToDateTime(row["WorkDate"]),
                    TicketNo = row["TicketNo"].ToString(),
                    StatusName = row["StatusName"].ToString()
                });
            }

            return result;
        }

        private string BuildShiftRegisterConflictMessage(List<ShiftRegisterConflictItem> conflicts)
        {
            if (conflicts == null || conflicts.Count == 0)
            {
                return "";
            }

            var display = conflicts.Take(5)
                .Select(x =>
                    $"{x.EmployeeCD} - {x.EmployeeName} ngày {x.WorkDate:dd/MM/yyyy} đã có phiếu {x.TicketNo} ({x.StatusName})")
                .ToList();

            string message = "Không thể đăng ký ca vì đã có phiếu đăng ký ca đang hiệu lực: "
                + string.Join("; ", display);

            if (conflicts.Count > 5)
            {
                message += $" và {conflicts.Count - 5} dòng khác.";
            }

            return message;
        }

        private MstShiftTypesModel DetectShiftByCheckIn(TimeSpan firstCheckIn, List<MstShiftTypesModel> shiftTypes)
        {
            if (shiftTypes == null || shiftTypes.Count == 0)
            {
                return null;
            }

            MstShiftTypesModel bestShift = null;
            double bestDiff = double.MaxValue;

            foreach (var shift in shiftTypes)
            {
                double diff = GetCircularMinuteDiff(firstCheckIn, shift.StartTime);

                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestShift = shift;
                }
            }

            /*
               Cho phép lệch tối đa 180 phút so với giờ bắt đầu ca.
               Ví dụ:
               Ca HC 07:50, nhân viên vào 08:20 vẫn thuộc HC.
               Ca 1 06:00, nhân viên vào 06:10 vẫn thuộc Ca 1.
            */
            if (bestDiff > 180)
            {
                return null;
            }
            return bestShift;
        }

        private double GetCircularMinuteDiff(TimeSpan a, TimeSpan b)
        {
            double diff = Math.Abs((a - b).TotalMinutes);

            if (diff > 720)
            {
                diff = 1440 - diff;
            }

            return diff;
        }

        private string FormatTime(TimeSpan? time)
        {
            return time.HasValue ? time.Value.ToString(@"hh\:mm") : "--";
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

        private class AttendancePunchItem
        {
            public string EmployeeCD { get; set; }

            public string EmployeeName { get; set; }

            public DateTime WorkDate { get; set; }

            public TimeSpan? FirstCheckIn { get; set; }

            public TimeSpan? LastCheckOut { get; set; }
        }

        private class AttendanceShiftMismatchItem
        {
            public string EmployeeCD { get; set; }

            public string EmployeeName { get; set; }

            public DateTime WorkDate { get; set; }

            public TimeSpan? FirstCheckIn { get; set; }

            public TimeSpan? LastCheckOut { get; set; }

            public string RequestedShiftName { get; set; }

            public string SuggestedShiftName { get; set; }
        }

        private class ShiftRegisterConflictItem
        {
            public string EmployeeCD { get; set; }

            public string EmployeeName { get; set; }

            public DateTime WorkDate { get; set; }

            public string TicketNo { get; set; }

            public string StatusName
            {
                get; set;
            }
        }
    }
}