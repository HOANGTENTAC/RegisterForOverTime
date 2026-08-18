using HRMS.Models;
using System.Collections.Generic;

namespace HRMS.ViewModels
{
    public class AccountPageViewModel
    {
        public string Keyword { get; set; }

        public string SelectedDept { get; set; }

        public int? SelectedAccessLevel { get; set; }

        public string SelectedStatus { get; set; }

        public List<DepartmentModel> Departments { get; set; }

        public List<AccountUserViewModel> Users { get; set; }

        public int TotalAccounts { get; set; }

        public int TotalAdmins { get; set; }

        public int TotalManagers { get; set; }

        public int TotalNoAccount { get; set; }
    }
}