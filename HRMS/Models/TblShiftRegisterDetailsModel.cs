using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("tbl_ShiftRegisterDetails")]
    public class TblShiftRegisterDetailsModel
    {
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Key]
        [Column("ShiftRegisterHeaderId")]
        public int ShiftRegisterHeaderId { get; set; }

        [Key]
        [Column("EmployeeCD")]
        public string EmployeeCD { get; set; }

        [Column("WorkDate")]
        public DateTime WorkDate { get; set; }

        [Column("ShiftTypeId")]
        public int ShiftTypeId { get; set; }
        

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }
    }
}