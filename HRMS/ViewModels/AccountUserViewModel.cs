using System;

namespace HRMS.ViewModels
{
    public class AccountUserViewModel
    {
        public string EmployeeCD { get; set; }

        public string TenNhanVien { get; set; }

        public string MaPhongBan { get; set; }

        public string TenPhongBan { get; set; }

        public bool HasAccount { get; set; }

        public int? HighestAccessLevel { get; set; }

        public string HighestAccessLevelName { get; set; }

        public string ManagedDepartmentsText { get; set; }

        public int ManagedDepartmentsCount { get; set; }

        public DateTime? NgayCapNhat { get; set; }

        public string TrangThai { get; set; }

        public bool YeuCauCapLaiMatKhau { get; set; }
    }
}