using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("NHANVIEN")]
    public class EmployeeModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã nhân viên")]
        [StringLength(10)]
        [Column("MaNhanVien")]
        public string MaNhanVien { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhân viên")]
        [StringLength(100)]
        [Column("TenNhanVien")]
        public string TenNhanVien { get; set; }

        [Key]
        [Column("MaChamCong")]
        public int MaChamCong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chấm công")]
        [StringLength(100)]
        [Column("TenChamCong")]
        public string TenChamCong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã thẻ")]
        [StringLength(20)]
        [Column("MaThe")]
        public string MaThe { get; set; }

        [StringLength(50)]
        [Column("UserPassWord")]
        public string UserPassWord { get; set; }

        [Column("PhanQuyen")]
        public int PhanQuyen { get; set; }

        [StringLength(10)]
        [Column("UserEnable")]
        public string UserEnable { get; set; }

        [Column("GioiTinh")]
        public bool GioiTinh { get; set; }

        [Column("NgayVaoLamViec")]
        public DateTime NgayVaoLamViec { get; set; }

        [StringLength(50)]
        [Column("ChucVu")]
        public string ChucVu { get; set; }

        [Column("NgaySinh")]
        public DateTime NgaySinh { get; set; }

        [StringLength(100)]
        [Column("NoiSinh")]
        public string NoiSinh { get; set; }

        [StringLength(20)]
        [Column("LoaiNhanVien")]
        public string LoaiNhanVien { get; set; }

        [Column("NgayKyHopDong")]
        public DateTime NgayKyHopDong { get; set; }

        [Column("ThoiHanHopDong")]
        public float ThoiHanHopDong { get; set; }

        [StringLength(50)]
        [Column("CMND")]
        public string CMND { get; set; }

        [Column("NgayCap")]
        public DateTime NgayCap { get; set; }

        [StringLength(100)]
        [Column("NoiCap")]
        public string NoiCap { get; set; }

        [StringLength(20)]
        [Column("DienThoaiLienHe")]
        public string DienThoaiLienHe { get; set; }

        [StringLength(50)]
        [Column("Email")]
        public string Email { get; set; }

        [Column("NgayPhep")]
        public float NgayPhep { get; set; }

        [Column("HinhAnh")]
        public byte[] HinhAnh { get; set; }

        [Column("TienLuong")]
        public decimal TienLuong { get; set; }

        [Column("LuongHopDong")]
        public decimal LuongHopDong { get; set; }

        [StringLength(30)]
        [Column("DanToc")]
        public string DanToc { get; set; }

        [StringLength(50)]
        [Column("QuocTich")]
        public string QuocTich { get; set; }

        [StringLength(50)]
        [Column("TrinhDo")]
        public string TrinhDo { get; set; }

        [StringLength(50)]
        [Column("Skype")]
        public string Skype { get; set; }

        [StringLength(50)]
        [Column("Yahoo")]
        public string Yahoo { get; set; }

        [StringLength(50)]
        [Column("Facebook")]
        public string Facebook { get; set; }

        [StringLength(7)]
        [Column("MaCongTy")]
        public string MaCongTy { get; set; }

        [StringLength(7)]
        [Column("MaKhuVuc")]
        public string MaKhuVuc { get; set; }

        [StringLength(7)]
        [Column("MaPhongBan")]
        public string MaPhongBan { get; set; }

        [NotMapped]
        public string TenPhongBan { get; set; }

        [StringLength(7)]
        [Column("MaChucVu")]
        public string MaChucVu { get; set; }

        [StringLength(50)]
        [Column("PassWord")]
        public string PassWord { get; set; }

        [Column("DangThamGiaBaoHiem")]
        public bool DangThamGiaBaoHiem { get; set; }

        [Column("NghiViecTamThoi")]
        public bool NghiViecTamThoi { get; set; }

        [Column("TinhLuongTheo")]
        public bool TinhLuongTheo { get; set; }

        [Column("SanPhamOrCongDoan")]
        public bool SanPhamOrCongDoan { get; set; }

        [Column("NhanVienMoi")]
        public bool NhanVienMoi { get; set; }

        [StringLength(100)]
        [Column("GhiChu")]
        public string GhiChu { get; set; }

        [NotMapped]
        public decimal MonthlyHours { get; set; }
    }
}