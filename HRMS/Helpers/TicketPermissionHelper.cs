using HRMS.Common;
using HRMS.Models;
using HRMS.Services;
using System;
using System.Data;
using System.Data.SqlClient;

namespace HRMS.Helpers
{
    public class TicketPermissionHelper
    {
        private readonly PermissionService _permissionService;
        public TicketPermissionHelper()
        {
            _permissionService = new PermissionService();
        }

        public bool CanEdit(int ticketId, UsersModel user, Enums.RequestTypeEnum requestType)
        {
            if (user == null)
            {
                return false;
            }

            // Check quyền Admin chung toàn hệ thống hoặc quyền admin riêng của loại ticket này
            if (_permissionService.CanViewAllData(user))
            {
                return true;
            }

            string tableHeader = string.Empty;

            switch ((int)requestType)
            {
                case 1:
                    tableHeader = "tbl_OvertimeHeaders";
                    break;
                case 7:
                    tableHeader = "tbl_ShiftRegisterHeaders";
                    break;
                default:
                    tableHeader = string.Empty;
                    break;
            }

            string sql = $@"SELECT TOP 1 t.CreatedUserCD, t.StatusId, h.ConfirmUserCD FROM tbl_Tickets t
                   INNER JOIN {tableHeader} h ON t.Id = h.TicketId
                   WHERE t.Id = @TicketId AND t.TicketTypeId = @TicketTypeId";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@TicketId", ticketId),
                new SqlParameter("@TicketTypeId", (int)requestType));

            if (dt.Rows.Count == 0)
            {
                return false;
            }

            string createdUser = dt.Rows[0]["CreatedUserCD"].ToString();
            int statusId = Convert.ToInt32(dt.Rows[0]["StatusId"]);

            bool isCreator = string.Equals(createdUser, user.MaNhanVien, StringComparison.OrdinalIgnoreCase);
            bool isAdmin = _permissionService.CanViewAllData(user);
            bool isConfirmUser = string.Equals(dt.Rows[0]["ConfirmUserCD"].ToString(), user.MaNhanVien, StringComparison.OrdinalIgnoreCase);
            bool isPending = statusId == (int)Enums.RequestStatusEnum.Pending;

            if (isPending == true && isConfirmUser == true)
            {
                return true;
            }

            return isCreator && isPending;
        }
    }
}