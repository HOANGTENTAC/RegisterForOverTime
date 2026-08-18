using HRMS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRMS.ViewModels
{
    public class OverTimePageViewModel
    {
        public IEnumerable<EmployeeModel> Employees { get; set; }

        public IEnumerable<UserRolesModel> UserRoles { get; set; }

        public OverTimeRequestModel Request { get; set; }

        public OverTimePageViewModel()
        {
            Employees = new List<EmployeeModel>();
            UserRoles = new List<UserRolesModel>();
            Request = new OverTimeRequestModel();
        }
    }

    public class OverTimeRequestModel
    {
        public int TicketId { get; set; }
        public string TicketNo { get; set; }
        public int OvertimeHeaderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DateRequest { get; set; }
        public DateTime FromTime { get; set; }
        public DateTime ToTime { get; set; }
        public int OvertimeType { get; set; }
        public string Reason { get; set; }
        public string CreatedUserCD { get; set; }
        public string ConfirmUserCD { get; set; }
        public string ConfirmUserName { get; set; }
        public bool AutoApprove { get; set; }
        public bool BreakFlag { get; set; }
        public decimal HoursWorked { get; set; }
        public bool ForceSubmit { get; set; }

        [Required(ErrorMessage = "Bạn phải chọn ít nhất 1 nhân viên")]

        //public List<string> EmployeeCDs { get; set; }
        public List<EmployeeModel> EmployeeCDs { get; set; }
    }
}