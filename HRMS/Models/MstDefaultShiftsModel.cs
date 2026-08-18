using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("mst_DefaultShifts")]
    public class MstDefaultShiftsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        [Column("DepartmentCD")]
        public string DepartmentCD { get; set; }

        [Column("ShiftTypeId")]
        public int ShiftTypeId { get; set; }

        [Column("EffectiveFrom")]
        public DateTime EffectiveFrom { get; set; }

        [Column("EffectiveTo")]
        public DateTime? EffectiveTo { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }
    }
}