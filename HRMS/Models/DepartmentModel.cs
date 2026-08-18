using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("PHONGBAN")]
    public class DepartmentModel
    {
        [Key]
        [Required(ErrorMessage = "Vui lòng nhập mã phòng ban")]
        [StringLength(7)]
        [Column("MaPhongBan")]
        public string MaPhongBan { get; set; }

        [StringLength(7)]
        [Column("MaCongTy")]
        public string MaCongTy { get; set; }

        [StringLength(7)]
        [Column("MaKhuVuc")]
        public string MaKhuVuc { get; set; }

        [StringLength(50)]
        [Column("TenPhongBan")]
        public string TenPhongBan { get; set; }

        [Column("SoTienSanLuong")]
        public decimal SoTienSanLuong { get; set; }
    }
}