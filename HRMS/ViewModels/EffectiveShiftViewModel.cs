using System;

namespace HRMS.ViewModels
{
    public class EffectiveShiftViewModel
    {
        public int ShiftTypeId { get; set; }

        public string ShiftCode { get; set; }

        public string ShiftName { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public int BreakMinutes { get; set; }

        public bool IsNightShift { get; set; }

        public string Source { get; set; }
    }
}