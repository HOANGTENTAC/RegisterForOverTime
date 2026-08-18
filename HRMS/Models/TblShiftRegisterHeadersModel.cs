using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("tbl_ShiftRegisterHeaders")]
    public class TblShiftRegisterHeadersModel
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

        [Column("FromDate")]
        public DateTime FromDate { get; set; }

        [Column("ToDate")]
        public DateTime ToDate { get; set; }

        [Column("ConfirmUserCD")]
        public string ConfirmUserCD { get; set; }

        [Column("Reason")]
        public string Reason { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [Column("UpdatedDate")]
        public DateTime? UpdatedDate { get; set; }

    }
}