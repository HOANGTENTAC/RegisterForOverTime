using HRMS.Models;
using System;
using System.Collections.Generic;

namespace HRMS.ViewModels
{
    public class HomePageViewModel
    {
        public UsersModel User { get; set; }

        public int WorkingDays { get; set; }

        public int CurrentMonth { get; set; }

        public int CurrentYear { get; set; }

        public decimal TotalHours { get; set; } = 0;

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public List<CalendarDayModel> Calendar { get; set; } = new List<CalendarDayModel>();

        public List<TblTicketsModel> RecentRequests { get; set; }

    }
}