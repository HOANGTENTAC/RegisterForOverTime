using HRMS.Models;
using System.Collections.Generic;

namespace HRMS.ViewModels
{
    public class EmployeePageViewModel
    {
        public string Keyword { get; set; }

        public string SelectedDept { get; set; }

        public string SelectedStatus { get; set; }

        public List<DepartmentModel> Departments { get; set; }

        public List<EmployeeListItemViewModel> Employees { get; set; }

        public int TotalEmployees { get; set; }

        public int TotalDepartments { get; set; }

        public int TotalNewEmployees { get; set; }

        public int TotalInsurance { get; set; }
    }
}