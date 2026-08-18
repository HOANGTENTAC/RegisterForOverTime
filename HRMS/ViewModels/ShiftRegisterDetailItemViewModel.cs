using System;

namespace HRMS.ViewModels
{
    public class ShiftRegisterDetailItemViewModel
    {
        public int Id { get; set; }

        public int ShiftRegisterHeaderId { get; set; }

        public string EmployeeCD { get; set; }

        public string EmployeeName { get; set; }

        public string MaPhongBan { get; set; }

        public string TenPhongBan { get; set; }

        public DateTime WorkDate { get; set; }

        public int ShiftTypeId { get; set; }

        public string ShiftCode { get; set; }

        public string ShiftName { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

    }
}
