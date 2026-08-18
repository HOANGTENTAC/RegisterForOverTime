namespace HRMS.ViewModels
{
    public class ReportOvertimeViewModel
    {
        public string TenPhongBan { get; set; }
        public string TenNhanVien { get; set; }
        public string EmployeeCD { get; set; }
        public decimal[] Ngay { get; set; } = new decimal[31];
        public decimal TongSoGioTangCa { get; set; }
        public bool IsTotalRow { get; set; }
    }
}