using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static HRMS.Common.Enums;

namespace HRMS.Models
{
    [Table("tbl_OvertimeHeaders")]
    public class TblOvertimeHeadersModel
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Key]
        [Column("TicketId")]
        public int TicketId { get; set; }

        [Column("RequestDate")]
        public DateTime RequestDate { get; set; }

        [Column("OvertimeType")]
        [Required(ErrorMessage = "Vui lòng chọn loại làm thêm")]
        public int OvertimeType { get; set; }

        public string OvertimeTypeName
        {
            get
            {
                switch (OvertimeType)
                {
                    case (int)OverTimeTypeEnum.OvertimeAfter:
                        return "Làm thêm giờ sau giờ làm việc";
                    case (int)OverTimeTypeEnum.OvertimeBefore:
                        return "Làm thêm giờ trước giờ làm việc";
                    case (int)OverTimeTypeEnum.OvertimeHolidays:
                        return "Làm thêm giờ vào ngày nghỉ";
                    default:
                        return "Không xác định";
                }
            }
        }

        [StringLength(10)]
        [Required(ErrorMessage = "Vui lòng chọn người duyệt")]
        [Column("ConfirmUserCD")]
        public string ConfirmUserCD { get; set; }

        [NotMapped]
        public string ConfirmUserName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn từ giờ")]
        [Column("FromTime")]
        public DateTime FromTime { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đến giờ")]
        [Column("ToTime")]
        public DateTime ToTime { get; set; } 

        [StringLength(255)]
        [Column("Reason")]
        public string Reason { get; set; }
    }
}