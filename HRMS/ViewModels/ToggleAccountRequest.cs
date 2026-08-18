using System.ComponentModel.DataAnnotations;

namespace HRMS.ViewModels
{
    public class ToggleAccountRequest
    {
        [Required]
        public string EmployeeCD { get; set; }

        public bool IsEnabled { get; set; }
    }
}