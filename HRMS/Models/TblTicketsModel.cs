using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Models
{
    [Table("tbl_Tickets")]
    public class TblTicketsModel
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(30)]
        [Required(ErrorMessage = "Vui lòng nhập số phiếu")]
        [Column("TicketNo")]
        public string TicketNo { get; set; }
        
        [Column("TicketTypeId")]
        [Required(ErrorMessage = "Vui lòng chọn loại phiếu")]
        public int TicketTypeId { get; set; }

        [NotMapped]
        public string TypeName { get; set; }

        [Column("StatusId")]
        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        public int StatusId { get; set; }

        [NotMapped]
        public string StatusName { get; set; } = string.Empty;

        [Column("CreatedUserCD")]
        [StringLength(10)]
        public string CreatedUserCD {  get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [Column("Reason")]
        [StringLength(255)]
        public string Reason { get; set; }

        [Column("UpdateUserCD")]
        [StringLength(10)]
        public string UpdateUserCD { get; set; }

        [Column("UpdateDate")]
        public DateTime? UpdateDate { get; set; }

        [NotMapped]
        public DateTime RequestDate { get; set; }

        [NotMapped]
        public string ReasonRequest { get; set; } = string.Empty;

    }
}