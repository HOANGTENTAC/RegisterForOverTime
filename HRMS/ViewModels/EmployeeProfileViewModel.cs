using System;

namespace HRMS.ViewModels
{
    public class EmployeeProfileViewModel
    {
        public string EmployeeCD { get; set; }

        public string TenNhanVien { get; set; }

        public int MaChamCong { get; set; }

        public string TenChamCong { get; set; }

        public string MaThe { get; set; }

        public string MaPhongBan { get; set; }

        public string TenPhongBan { get; set; }

        public string ChucVu { get; set; }

        public string MaChucVu { get; set; }

        public bool GioiTinh { get; set; }

        public DateTime? NgaySinh { get; set; }

        public string NoiSinh { get; set; }

        public DateTime? NgayVaoLamViec { get; set; }

        public string LoaiNhanVien { get; set; }

        public DateTime? NgayKyHopDong { get; set; }

        public float ThoiHanHopDong { get; set; }

        public string CMND { get; set; }

        public DateTime? NgayCap { get; set; }

        public string NoiCap { get; set; }

        public string DienThoaiLienHe { get; set; }

        public string Email { get; set; }

        public float NgayPhep { get; set; }

        public string DanToc { get; set; }

        public string QuocTich { get; set; }

        public string TrinhDo { get; set; }

        public string MaCongTy { get; set; }

        public string MaKhuVuc { get; set; }

        public bool DangThamGiaBaoHiem { get; set; }

        public bool NghiViecTamThoi { get; set; }

        public bool NhanVienMoi { get; set; }

        public string GhiChu { get; set; }

        public string UserEnable { get; set; }

        public string TrangThai { get; set; }

        public string PhotoDataUrl { get; set; }

        public bool HasPhoto { get; set; }
    }
}