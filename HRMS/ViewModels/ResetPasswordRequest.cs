using System.ComponentModel.DataAnnotations;

namespace HRMS.ViewModels
{
    public class ResetPasswordRequest
    {
        [Required]
        public string EmployeeCD { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 4)]
        public string NewPassword { get; set; }
    }
}