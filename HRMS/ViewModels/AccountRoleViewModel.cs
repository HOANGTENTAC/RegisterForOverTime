using System;

namespace HRMS.ViewModels
{
    public class AccountRoleViewModel
    {
        public string EmployeeCD { get; set; }

        public string TenNhanVien { get; set; }

        public string BoPhanQuanLy { get; set; }

        public string TenBoPhanQuanLy { get; set; }

        public int AccessLevel { get; set; }

        public string AccessLevelName { get; set; }

        public DateTime? NgayCapNhat { get; set; }
    }
}