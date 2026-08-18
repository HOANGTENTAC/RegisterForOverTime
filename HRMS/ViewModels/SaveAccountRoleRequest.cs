using System.ComponentModel.DataAnnotations;

namespace HRMS.ViewModels
{
    public class SaveAccountRoleRequest
    {
        [Required]
        public string EmployeeCD { get; set; }

        [Required]
        public string BoPhanQuanLy { get; set; }

        [Required]
        public int AccessLevel { get; set; }
    }
}