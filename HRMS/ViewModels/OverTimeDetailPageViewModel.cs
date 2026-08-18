using HRMS.Models;
using System.Collections.Generic;

namespace HRMS.ViewModels
{
    public class OverTimeDetailPageViewModel
    {
        public TblTicketsModel Ticket { get; set; }
        public TblOvertimeHeadersModel Header { get; set; }
        public List<TblOvertimeDetailsModel> Details { get; set; }
    }
}