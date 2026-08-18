using System;

namespace HRMS.ViewModels
{
    public class AttendanceDayCellViewModel
    {
        public int Day { get; set; }

        public DateTime Date { get; set; }

        public DateTime? FirstCheckIn { get; set; }

        public DateTime? LastCheckOut { get; set; }

        public decimal WorkingHours { get; set; }

        public bool IsOffDay { get; set; }

        public bool IsHoliday { get; set; }

        public string StatusCode { get; set; }

        public string StatusText { get; set; }

        public string Symbol { get; set; }

        public string Note { get; set; }

        public int? ShiftTypeId { get; set; }

        public string ShiftCode { get; set; }

        public string ShiftName { get; set; }

        public string ShiftSource { get; set; }

        public string ShiftTimeText { get; set; }
    }
}