using System.ComponentModel.DataAnnotations;

namespace HRMS.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã nhân viên")]
        [StringLength(10)]
        public string EmployeeCD { get; set; }

    }
}