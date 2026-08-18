using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("tbl_OvertimeDetails")]
    public class TblOvertimeDetailsModel
    {
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Key]
        [Column("OvertimeHeaderId")]
        public int OvertimeHeaderId { get; set; }

        [Key]
        [Required(ErrorMessage = "Vui lòng chọn mã nhân viên")]
        [StringLength(10)]
        public string EmployeeCD { get; set; }

        [NotMapped]
        public string EmployeeName { get; set; }

        [Column("BreakFlag")]
        [Required(ErrorMessage = "Vui lòng chọn có nghỉ giữa giờ hay không")]
        public bool BreakFlag {get; set;}

        [Column("OvertimeDate")]
        public DateTime OvertimeDate { get; set; }

        [Column("HoursWorked")] 
        public decimal HoursWorked { get; set; }

        [NotMapped]
        public decimal MonthlyHours { get; set; }

    }
}