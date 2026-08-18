using System;

namespace HRMS.ViewModels
{
    public class EmployeeListItemViewModel
    {
        public string EmployeeCD { get; set; }

        public string TenNhanVien { get; set; }

        public int MaChamCong { get; set; }

        public string MaThe { get; set; }

        public string MaPhongBan { get; set; }

        public string TenPhongBan { get; set; }

        public string ChucVu { get; set; }

        public string LoaiNhanVien { get; set; }

        public DateTime? NgayVaoLamViec { get; set; }

        public bool DangThamGiaBaoHiem { get; set; }

        public bool NhanVienMoi { get; set; }

        public bool NghiViecTamThoi { get; set; }

        public string UserEnable { get; set; }

        public string TrangThai { get; set; }
    }
}