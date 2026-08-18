using System;

namespace HRMS.ViewModels
{
    public class ShiftRegisterHeaderDetailViewModel
    {
        public int Id { get; set; }

        public int TicketId { get; set; }

        public DateTime RequestDate { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public string ConfirmUserCD { get; set; }

        public string ConfirmUserName { get; set; }

        public string Reason { get; set; }
    }
}