using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("UserRoles")]
    public class UserRolesModel
    {
        [Column("MaNhanVien")]
        public string MaNhanVien { get; set; }

        [Column("BoPhanQuanLy")]
        public string BoPhanQuanLy { get; set; }

        [NotMapped]
        public string TenPhongBan { get; set; }

        [NotMapped]
        public string TenNhanVien { get; set; }

        [Column("AccessLevel")]
        public int AccessLevel { get; set; }

        [Column("NgayCapNhat")]
        public DateTime NgayCapNhat { get; set; }
    }
}