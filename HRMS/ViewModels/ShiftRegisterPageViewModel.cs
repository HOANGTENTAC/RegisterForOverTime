using HRMS.Models;
using System.Collections.Generic;

namespace HRMS.ViewModels
{
    public class ShiftRegisterPageViewModel
    {
        public ShiftRegisterRequestModel Request { get; set; }

        public List<EmployeeModel> Employees { get; set; }

        public List<UserRolesModel> UserRoles { get; set; }

        public List<MstShiftTypesModel> ShiftTypes { get; set; }

        public ShiftRegisterPageViewModel()
        {
            Request = new ShiftRegisterRequestModel();
            Employees = new List<EmployeeModel>();
            UserRoles = new List<UserRolesModel>();
            ShiftTypes = new List<MstShiftTypesModel>();
        }
    }
}