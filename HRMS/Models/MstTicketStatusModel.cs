using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("mst_TicketStatus")]
    public class MstTicketStatusModel
    {
        [Key]
        [Column("StatusId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StatusId { get; set; }

        [Column("StatusCode")]
        [StringLength(50)]
        public string StatusCode { get; set; }

        [Column("StatusName")]
        [StringLength(100)]
        public string StatusName { get; set; }

        [Column("StatusColor")]
        [StringLength(20)]
        public string StatusColor { get; set; }

    }
}