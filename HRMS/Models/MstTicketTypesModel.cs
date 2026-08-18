using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("mst_TicketTypes")]
    public class MstTicketTypesModel
    {
        [Column("TicketTypeId")]
        public int TicketTypeId { get; set; }

        [Key]
        [Column("TypeCode")]
        [StringLength(50)]
        public string TypeCode { get; set; }

        [Column("TypeName")]
        [StringLength(100)]
        public string TypeName { get; set; }

        [Column("IconClass")]
        [StringLength(100)]
        public string IconClass { get; set; }
    }
}