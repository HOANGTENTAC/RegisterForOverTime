namespace HRMS.ViewModels
{
    public class ReportOvertimeYearViewModel
    {
        public string EmployeeCD { get; set; }

        public string TenNhanVien { get; set; }

        public string TenPhongBan { get; set; }

        public decimal[] Thang { get; set; } = new decimal[12];

        public decimal TongCong { get; set; }

        public bool IsTotalRow { get; set; }
    }
}