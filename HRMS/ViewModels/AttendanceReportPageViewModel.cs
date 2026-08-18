using HRMS.Models;
using System.Collections.Generic;

namespace HRMS.ViewModels
{
    public class AttendanceReportPageViewModel
    {
        public string SelectedMonth { get; set; }

        public string SelectedDept { get; set; }

        public string SelectedEmployee { get; set; }

        public string SelectedStatus { get; set; }

        public List<DepartmentModel> Departments { get; set; }

        public List<AttendanceEmployeeRowViewModel> Rows { get; set; }

        public int TotalEmployees { get; set; }

        public int TotalLateIn { get; set; }

        public int TotalEarlyOut { get; set; }

        public int TotalMissing { get; set; }

        public int TotalWorkOnOffDay { get; set; }
    }
}