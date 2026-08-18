using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("mst_ShiftTypes")]
    public class MstShiftTypesModel
    {
        [Column("ShiftTypeId")]
        public int ShiftTypeId { get; set; }

        [Key]
        [Column("ShiftCode")]
        public string ShiftCode { get; set; }

        [Column("ShiftName")]
        public string ShiftName { get; set; }

        [Column("StartTime")]
        public TimeSpan StartTime { get; set; }

        [Column("EndTime")]
        public TimeSpan EndTime { get; set; }

        [Column("BreakMinutes")]
        public int BreakMinutes { get; set; }

        [Column("IsNightShift")]
        public bool IsNightShift { get; set; }

    }
}