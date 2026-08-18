namespace HRMS.ViewModels
{
    public class AttendanceEmployeeRowViewModel
    {
        public string MaPhongBan { get; set; }

        public string TenPhongBan { get; set; }

        public string EmployeeCD { get; set; }

        public string TenNhanVien { get; set; }

        public AttendanceDayCellViewModel[] Days { get; set; } =
            new AttendanceDayCellViewModel[31];

        public int WorkingDays { get; set; }

        public decimal TotalHours { get; set; }

        public int IssueCount { get; set; }
    }
}