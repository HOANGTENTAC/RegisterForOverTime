using HRMS.Common;
using HRMS.Filters;
using HRMS.Models;
using System;
using System.Collections.Generic;

namespace HRMS.ViewModels
{
    public class ShiftRegisterDetailPageViewModel
    {
        public TblTicketsModel Ticket { get; set; }

        public ShiftRegisterHeaderDetailViewModel Header { get; set; }

        public List<ShiftRegisterDetailItemViewModel> Details { get; set; }

        public ShiftRegisterDetailPageViewModel()
        {
            Details = new List<ShiftRegisterDetailItemViewModel>();
        }

        public TicketPermissions GetPermissions(string currentUserCD, bool canAdmin, bool canHrProcess, bool canEdit)
        {
            bool isPending = Ticket.StatusId == (int)Enums.RequestStatusEnum.Pending;

            bool isManagerAccepted = Ticket.StatusId == (int)Enums.RequestStatusEnum.ManagerAccepted;

            bool isCreator = !string.IsNullOrEmpty(currentUserCD) &&
                string.Equals(currentUserCD, Ticket.CreatedUserCD, StringComparison.OrdinalIgnoreCase);

            bool isConfirmUser = !string.IsNullOrEmpty(currentUserCD) &&
                string.Equals(currentUserCD, Header.ConfirmUserCD, StringComparison.OrdinalIgnoreCase);

            bool isCancelled = Ticket.StatusId == (int)Enums.RequestStatusEnum.Cancelled;

            if(isCancelled == true)
            {
                return new TicketPermissions
                {
                    CanManagerApproveReject = false,
                    CanHrFinishReject = false,
                    CanCancelTicket = false,
                    CanEditTicket = false,
                    CanRemoveDetail = false
                };
            }

            return new TicketPermissions
            {
                /*
                   Quản lý duyệt/từ chối:
                   Chỉ người xác nhận được thao tác khi phiếu PENDING.
                   Admin/HR không đi luồng manager approve/reject nữa.
                */
                CanManagerApproveReject = isPending && isConfirmUser,

                /*
                   HR xử lý:
                   HR = Admin hoặc AccessLevel 5.
                   Chỉ xử lý khi quản lý đã duyệt.
                */
                CanHrFinishReject = isManagerAccepted && canHrProcess,

                /*
                   Cancel:
                   Chỉ Admin hoặc AccessLevel 5.
                   Đây là hủy trạng thái, không xóa vật lý.
                */
                CanCancelTicket = canAdmin,

                /*
                   Edit:
                   Controller đã quyết định quyền edit.
                   ViewModel chỉ dùng kết quả đó.
                */
                CanEditTicket = canEdit,

                /*
                   Remove detail:
                   Admin/HR được remove.
                   Người tạo hoặc người xác nhận chỉ remove khi PENDING.
                */
                CanRemoveDetail = canAdmin || (isPending && (isCreator || isConfirmUser))
            };
        }
    }
}