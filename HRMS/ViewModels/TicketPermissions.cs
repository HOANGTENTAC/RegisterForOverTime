namespace HRMS.ViewModels
{
    public class TicketPermissions
    {
        public bool CanManagerApproveReject { get; set; }

        public bool CanHrFinishReject { get; set; }

        public bool CanCancelTicket { get; set; }

        public bool CanRemoveDetail { get; set; }

        public bool CanEditTicket { get; set; }
    }
}