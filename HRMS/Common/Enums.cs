namespace HRMS.Common
{
    public class Enums
    {
        public enum RequestTypeEnum
        {
            OT = 1,
            LV = 2,
            BT = 3,
            WH = 4,
            SC = 5,
            TA = 6,
            SR = 7,
        }

        public enum RequestStatusEnum
        {
            Draft = 0,
            Pending = 10,
            ManagerAccepted = 20,
            ManagerRejected = 30,
            HrRejected = 40,
            Finished = 90,
            Cancelled = 99
        }

        public enum OverTimeTypeEnum
        {
            OvertimeBefore = 0,
            OvertimeAfter = 1,
            OvertimeHolidays = 2
        }
    }
}