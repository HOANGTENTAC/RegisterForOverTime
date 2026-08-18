using System;

namespace HRMS.Models
{
    public class AttendanceRawItem
    {
        public int PunchCount { get; set; }

        public DateTime? FirstCheckIn { get; set; }

        public DateTime? LastCheckOut { get; set; }
    }
}