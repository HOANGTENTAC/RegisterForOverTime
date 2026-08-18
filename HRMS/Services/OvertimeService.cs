using HRMS.Common;
using HRMS.Helpers;
using HRMS.Models;
using HRMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HRMS.Services
{
    public class OvertimeService
    {
        private readonly ApplicationDbContext _context;
        public OvertimeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public ServiceResult CreateOvertimeRequest(OverTimeRequestModel request, bool isDayOff)
        {
            if (!CheckValidateOverTimeStart(request.FromTime, request.ToTime))
            {
                return new ServiceResult { Success = false, Message = "Giờ bắt đầu không được lớn hơn giờ kết thúc!" };
            }

            if (!CheckValidateOverTimeEnd(request.FromTime, request.ToTime))
            {
                return new ServiceResult { Success = false, Message = "Giờ kết thúc không được nhỏ hơn giờ bắt đầu!" };
            }
            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    // Insert Ticket
                    var ticket = new TblTicketsModel
                    {
                        TicketNo = GenerateHelper.GenerateRequestNo((int)Enums.RequestTypeEnum.OT),
                        TicketTypeId = (int)Enums.RequestTypeEnum.OT,
                        StatusId = request.AutoApprove ?
                                (int)Enums.RequestStatusEnum.ManagerAccepted :
                                (int)Enums.RequestStatusEnum.Pending,
                        CreatedUserCD = request.CreatedUserCD,
                        CreatedDate = DateTime.Now,
                        UpdateUserCD = request.CreatedUserCD,
                        UpdateDate = DateTime.Now
                    };
                    _context.TblTickets.Add(ticket);
                    _context.SaveChanges();
                    request.TicketId = ticket.Id;

                    // Insert Header
                    request.OvertimeType = CheckTypeOverTime(request.FromTime, request.ToTime, isDayOff);
                    var header = new TblOvertimeHeadersModel
                    {
                        TicketId = request.TicketId,
                        RequestDate = request.DateRequest,
                        OvertimeType = request.OvertimeType,
                        ConfirmUserCD = request.ConfirmUserCD,
                        FromTime = request.FromTime,
                        ToTime = request.ToTime,
                        Reason = request.Reason
                    };
                    _context.TblOvertimeHeaders.Add(header);
                    _context.SaveChanges();
                    request.OvertimeHeaderId = header.Id;

                    request.HoursWorked = (decimal)(request.ToTime - request.FromTime).TotalMinutes / 60;
                    if (request.OvertimeType == 2)
                    {
                        request.HoursWorked -= 40m / 60m; // trừ 40 phút nghỉ trưa
                    }
                    request.HoursWorked = RoundDownToQuarterHour(request.HoursWorked);
                    request.BreakFlag = CheckBreakTime(request.FromTime, request.ToTime);

                    // Insert Details
                    var selectedEmployees = request.EmployeeCDs
                               .Where(e => !string.IsNullOrEmpty(e.MaNhanVien))
                               .ToList();
                    foreach (var empCd in selectedEmployees)
                    {
                        bool exists = (from d in _context.TblOvertimeDetails
                                       join h in _context.TblOvertimeHeaders on d.OvertimeHeaderId equals h.Id
                                       join t in _context.TblTickets on h.TicketId equals t.Id
                                       where d.EmployeeCD == empCd.MaNhanVien
                                             && h.RequestDate == request.DateRequest
                                             && t.StatusId == (int)Enums.RequestStatusEnum.Pending
                                             && h.OvertimeType == request.OvertimeType
                                       select d).Any();

                        if (exists == true)
                        {
                            return new ServiceResult
                            {
                                Success = false,
                                Message = $"Mã nhân viên {empCd.MaNhanVien}" +
                                $" đã đăng ký tăng ca ngày {request.DateRequest:dd/MM/yyyy}"
                            };
                        }
                        var detail = new TblOvertimeDetailsModel
                        {
                            OvertimeHeaderId = request.OvertimeHeaderId,
                            BreakFlag = request.BreakFlag,
                            EmployeeCD = empCd.MaNhanVien,
                            OvertimeDate = request.DateRequest,
                            HoursWorked = request.HoursWorked
                        };
                        _context.TblOvertimeDetails.Add(detail);
                    }
                    _context.SaveChanges();
                    tran.Commit();
                    return new ServiceResult { Success = true, Message = "Đăng ký thành công" };
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return new ServiceResult { Success = false, Message = ex.Message };
                }
            }
        }

        public ServiceResult UpdateOvertimeRequest(OverTimeRequestModel request, bool isDayOff)
        {
            if (!CheckValidateOverTimeStart(request.FromTime, request.ToTime))
            {
                return new ServiceResult { Success = false, Message = "Giờ bắt đầu không được lớn hơn giờ kết thúc!" };
            }
            if (!CheckValidateOverTimeEnd(request.FromTime, request.ToTime))
            {
                return new ServiceResult { Success = false, Message = "Giờ kết thúc không được nhỏ hơn giờ bắt đầu!" };
            }
            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var ticket = _context.TblTickets.FirstOrDefault(t => t.Id == request.TicketId);
                    if (ticket == null)
                    {
                        return new ServiceResult { Success = false, Message = "Không tìm thấy yêu cầu tăng ca" };
                    }
                    if (ticket.StatusId != (int)Enums.RequestStatusEnum.Pending)
                    {
                        return new ServiceResult { Success = false, Message = "Yêu cầu tăng ca đã được xử lý trước đó" };
                    }
                    // Update Header
                    var header = _context.TblOvertimeHeaders.FirstOrDefault(h => h.TicketId == request.TicketId);
                    header.RequestDate = request.DateRequest;
                    header.OvertimeType = CheckTypeOverTime(request.FromTime, request.ToTime, isDayOff);
                    header.ConfirmUserCD = request.ConfirmUserCD;
                    header.FromTime = request.FromTime;
                    header.ToTime = request.ToTime;
                    header.Reason = request.Reason;
                    _context.SaveChanges();

                    // Update Details
                    var existingDetails = _context.TblOvertimeDetails.Where(d => d.OvertimeHeaderId == header.Id).ToList();
                    _context.TblOvertimeDetails.RemoveRange(existingDetails);
                    _context.SaveChanges();
                    request.HoursWorked = (decimal)(request.ToTime - request.FromTime).TotalMinutes / 60;
                    if (request.OvertimeType == 2)
                    {
                        request.HoursWorked -= 40m / 60m; // trừ 40 phút nghỉ trưa
                    }
                    request.HoursWorked = RoundDownToQuarterHour(request.HoursWorked);
                    request.BreakFlag = CheckBreakTime(request.FromTime, request.ToTime);
                    var selectedEmployees = request.EmployeeCDs
                                .Where(e => !string.IsNullOrEmpty(e.MaNhanVien))
                                .ToList();
                    foreach (var empCd in selectedEmployees)
                    {
                        bool exists = (from d in _context.TblOvertimeDetails
                                       join h in _context.TblOvertimeHeaders on d.OvertimeHeaderId equals h.Id
                                       join t in _context.TblTickets on h.TicketId equals t.Id
                                       where d.EmployeeCD == empCd.MaNhanVien
                                             && h.RequestDate == request.DateRequest
                                             && t.StatusId == (int)Enums.RequestStatusEnum.Pending
                                             && h.OvertimeType == request.OvertimeType
                                       select d).Any();
                        if (exists == true)
                        {
                            return new ServiceResult
                            {
                                Success = false,
                                Message = $"Mã nhân viên {empCd.MaNhanVien}" +
                                $" đã đăng ký tăng ca ngày {request.DateRequest:dd/MM/yyyy}"
                            };
                        }
                        var detail = new TblOvertimeDetailsModel
                        {
                            OvertimeHeaderId = header.Id,
                            BreakFlag = request.BreakFlag,
                            EmployeeCD = empCd.MaNhanVien,
                            OvertimeDate = request.DateRequest,
                            HoursWorked = request.HoursWorked
                        };
                        _context.TblOvertimeDetails.Add(detail);
                    }
                    _context.SaveChanges();
                    tran.Commit();
                    return new ServiceResult { Success = true, Message = "Đã lưu thành công" };
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return new ServiceResult { Success = false, Message = ex.Message };
                }
            }
        }

        public ServiceResult DeleteOvertimeRequest(int ticketId, string userCD, bool canForceDelete = false)
        {
            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var ticket = _context.TblTickets.FirstOrDefault(t => t.Id == ticketId);

                    if (ticket == null)
                    {
                        return new ServiceResult
                        {
                            Success = false,
                            Message = "Không tìm thấy yêu cầu tăng ca"
                        };
                    }

                    if (!canForceDelete && ticket.StatusId != (int)Enums.RequestStatusEnum.Pending)
                    {
                        return new ServiceResult
                        {
                            Success = false,
                            Message = "Yêu cầu tăng ca đã được xử lý trước đó"
                        };
                    }

                    if (!canForceDelete && ticket.CreatedUserCD != userCD)
                    {
                        return new ServiceResult
                        {
                            Success = false,
                            Message = "Bạn không có quyền xóa yêu cầu này"
                        };
                    }

                    var header = _context.TblOvertimeHeaders.FirstOrDefault(h => h.TicketId == ticketId);

                    if (header == null)
                    {
                        return new ServiceResult
                        {
                            Success = false,
                            Message = "Không tìm thấy thông tin tăng ca"
                        };
                    }

                    var details = _context.TblOvertimeDetails
                        .Where(d => d.OvertimeHeaderId == header.Id)
                        .ToList();

                    _context.TblOvertimeDetails.RemoveRange(details);
                    _context.TblOvertimeHeaders.Remove(header);
                    _context.TblTickets.Remove(ticket);

                    _context.SaveChanges();
                    tran.Commit();

                    return new ServiceResult
                    {
                        Success = true,
                        Message = "Đã xóa thành công"
                    };
                }
                catch (Exception ex)
                {
                    tran.Rollback();

                    return new ServiceResult
                    {
                        Success = false,
                        Message = ex.Message
                    };
                }
            }
        }

        public ServiceResult ApproveOvertimeRequest(int ticketId, string approverUserCD, bool isApproved,
            string reason = null, bool canForceApprove = false)
        {
            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var ticket = _context.TblTickets.FirstOrDefault(t => t.Id == ticketId);
                    var header = _context.TblOvertimeHeaders.FirstOrDefault(h => h.TicketId == ticketId);

                    if (ticket == null || header == null)
                    {
                        return new ServiceResult
                        {
                            Success = false,
                            Message = "Không tìm thấy yêu cầu tăng ca"
                        };
                    }

                    if (!canForceApprove && header.ConfirmUserCD != approverUserCD)
                    {
                        return new ServiceResult
                        {
                            Success = false,
                            Message = "Bạn không có quyền duyệt yêu cầu này"
                        };
                    }

                    if (!canForceApprove && ticket.StatusId != (int)Enums.RequestStatusEnum.Pending)
                    {
                        return new ServiceResult
                        {
                            Success = false,
                            Message = "Yêu cầu tăng ca đã được xử lý trước đó"
                        };
                    }

                    if (isApproved == false && string.IsNullOrWhiteSpace(reason))
                    {
                        return new ServiceResult
                        {
                            Success = false,
                            Message = "Bạn vui lòng nhập lý do từ chối"
                        };
                    }

                    ticket.StatusId = isApproved ? (int)Enums.RequestStatusEnum.ManagerAccepted : (int)Enums.RequestStatusEnum.ManagerRejected;

                    ticket.Reason = isApproved ? null : reason;
                    ticket.UpdateDate = DateTime.Now;
                    ticket.UpdateUserCD = approverUserCD;

                    _context.SaveChanges();
                    tran.Commit();

                    return new ServiceResult
                    {
                        Success = true,
                        Message = isApproved ? "Yêu cầu tăng ca đã được duyệt" : "Yêu cầu tăng ca đã bị từ chối"
                    };
                }
                catch (Exception ex)
                {
                    tran.Rollback();

                    return new ServiceResult
                    {
                        Success = false,
                        Message = ex.Message
                    };
                }
            }
        }

        private decimal RoundDownToQuarterHour(decimal hours)
        {
            // Tính số bậc 0.25 gần nhất nhưng không vượt quá hours
            decimal step = 0.25m;
            decimal result = Math.Floor(hours / step) * step;
            return result;
        }

        private bool CheckValidateOverTimeStart(DateTime timeStart, DateTime timeEnd)
        {
            if (timeStart.TimeOfDay > timeEnd.TimeOfDay)
            {
                return false;
            }
            return true;
        }

        private bool CheckValidateOverTimeEnd(DateTime timeStart, DateTime timeEnd)
        {
            if (timeEnd.TimeOfDay < timeStart.TimeOfDay)
            {
                return false;
            }
            return true;
        }

        public int CheckTypeOverTime(DateTime start, DateTime end, bool isDayOff)
        {
            if (isDayOff == true)
            {
                return 2;
            }
            else
            {
                var startTime = start.TimeOfDay;
                var endTime = end.TimeOfDay;

                // Định nghĩa các ca
                var shifts = new List<(TimeSpan start, TimeSpan end)>
                {
                    (new TimeSpan(7,50,0), new TimeSpan(16,30,0)), // hành chính
                    (new TimeSpan(6,0,0), new TimeSpan(14,0,0)),   // ca 1
                    (new TimeSpan(14,0,0), new TimeSpan(22,0,0)),  // ca 2
                    (new TimeSpan(22,0,0), new TimeSpan(6,0,0))    // ca 3 (qua ngày)
                };

                foreach (var shift in shifts)
                {
                    // Ca 3 (22h–06h)
                    if (shift.start > shift.end)
                    {
                        if (startTime >= shift.start || startTime < shift.end)
                        {
                            if (startTime < shift.start)
                            {
                                return 0; // trước
                            }
                            if (endTime > shift.end)
                            {
                                return 1;     // sau
                            }
                            return -1; // trong ca
                        }
                    }
                    else
                    {
                        if (startTime >= shift.start && endTime <= shift.end)
                        {
                            return -1; // trong ca
                        }
                        if (startTime < shift.start && endTime <= shift.start)
                        {
                            return 0; // trước
                        }
                        if (startTime >= shift.start && endTime > shift.end)
                        {
                            return 1; // sau
                        }
                    }
                }
            }
            return -1; // Trường hợp không xác định
        }

        private bool CheckBreakTime(DateTime start, DateTime end)
        {
            // Giờ nghỉ trưa: 11h50 – 12h30
            var breakStart = new TimeSpan(11, 50, 0);
            var breakEnd = new TimeSpan(12, 30, 0);
            var startTime = start.TimeOfDay;
            var endTime = end.TimeOfDay;
            if (startTime < breakStart && endTime > breakStart)
            {
                return true; // Có giờ nghỉ trưa
            }
            else if (startTime < breakEnd && endTime > breakEnd)
            {
                return true; // Có giờ nghỉ trưa
            }
            else if (startTime >= breakStart && endTime <= breakEnd)
            {
                return true; // Toàn bộ thời gian nằm trong giờ nghỉ trưa
            }
            return false; // Không có giờ nghỉ trưa
        }
    }

}