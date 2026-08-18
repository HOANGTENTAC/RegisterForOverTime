using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("Users")]
    public class UsersModel
    {
        [Key]
        [Required(ErrorMessage = "Vui lòng nhập mã nhân viên")]
        [StringLength(10)]
        [Column("MaNhanVien")]
        public string MaNhanVien { get; set; }

        [NotMapped]
        public int MaChamCong { get; set; }

        [NotMapped]
        public string TenNhanVien { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Column("MatKhau")]
        public string MatKhau { get; set; }

        [Column("YeuCauCapLaiMatKhau")]
        public bool YeuCauCapLaiMatKhau { get; set; } = false;

        [NotMapped]
        public string Avatar { get; set; }

        [NotMapped]
        public string MaPhongBan { get; set; }

        [NotMapped]
        public string TenPhongBan { get; set; }

        [Column("IsAdmin")]
        public bool IsAdmin { get; set; } = false;

        [NotMapped]
        public bool RememberMe { get; set; }

        [Column("NgayCapNhat")]
        public DateTime NgayCapNhat { get; set; }
    }
}