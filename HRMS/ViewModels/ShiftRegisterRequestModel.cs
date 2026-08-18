using HRMS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRMS.ViewModels
{
    public class ShiftRegisterRequestModel
    {
        public int TicketId { get; set; }

        public int HeaderId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        public DateTime ToDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ca làm việc")]
        public int ShiftTypeId { get; set; }

        public string ConfirmUserCD { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string CreatedUserCD { get; set; }

        public bool CreateAsManagerAccepted { get; set; }

        public bool CreateAsFinished { get; set; }

        public List<EmployeeModel> EmployeeCDs { get; set; }

        public ShiftRegisterRequestModel()
        {
            EmployeeCDs = new List<EmployeeModel>();
        }
    }
}