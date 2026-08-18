using System;
using System.Collections.Generic;
using HRMS.Models;

namespace HRMS.ViewModels
{
    public class DefaultShiftPageViewModel
    {
        public List<DefaultShiftRowViewModel> Rows { get; set; }

        public List<DepartmentOptionViewModel> Departments { get; set; }

        public List<MstShiftTypesModel> ShiftTypes { get; set; }

        public DefaultShiftFormViewModel Form { get; set; }

        public DefaultShiftPageViewModel()
        {
            Rows = new List<DefaultShiftRowViewModel>();
            Departments = new List<DepartmentOptionViewModel>();
            ShiftTypes = new List<MstShiftTypesModel>();
            Form = new DefaultShiftFormViewModel();
        }
    }

    public class DefaultShiftRowViewModel
    {
        public int Id { get; set; }

        public string DepartmentCD { get; set; }

        public string DepartmentName { get; set; }

        public int ShiftTypeId { get; set; }

        public string ShiftCode { get; set; }

        public string ShiftName { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }
    }

    public class DefaultShiftFormViewModel
    {
        public int Id { get; set; }

        public string DepartmentCD { get; set; }

        public int ShiftTypeId { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; }
    }

    public class DepartmentOptionViewModel
    {
        public string MaPhongBan { get; set; }

        public string TenPhongBan { get; set; }
    }
}