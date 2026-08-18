using System;

namespace HRMS.Models
{
    public class CalendarDayModel
    {
        public int DayNumber { get; set; }

        public bool IsOff { get; set; }      // true = ngày nghỉ trong bảng NgayNghi

        public bool IsToday { get; set; }

        public bool IsHoliday { get; set; }  // Holiday code cứng

        public DayOfWeek DayOfWeek { get; set; }

        public string Note { get; set; }
    }
}