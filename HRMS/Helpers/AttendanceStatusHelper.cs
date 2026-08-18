using HRMS.Common;

namespace HRMS.Helpers
{
    public static class AttendanceStatusHelper
    {
        public static string GetText(string statusCode)
        {
            switch (statusCode)
            {
                case AttendanceStatusCode.Ok:
                    return "Đủ công";

                case AttendanceStatusCode.LateIn:
                    return "Vào trễ";

                case AttendanceStatusCode.EarlyOut:
                    return "Ra sớm";

                case AttendanceStatusCode.MissingIn:
                    return "Thiếu check-in";

                case AttendanceStatusCode.MissingOut:
                    return "Thiếu check-out";

                case AttendanceStatusCode.NoData:
                    return "Không có dữ liệu";

                case AttendanceStatusCode.Off:
                    return "Ngày nghỉ";

                case AttendanceStatusCode.Holiday:
                    return "Ngày lễ";

                case AttendanceStatusCode.WorkOnOffDay:
                    return "Có chấm công ngày nghỉ";

                case AttendanceStatusCode.Future:
                    return "Chưa tới ngày";

                default:
                    return "";
            }
        }

        public static string GetSymbol(string statusCode)
        {
            switch (statusCode)
            {
                case AttendanceStatusCode.Ok:
                    return "✓";

                case AttendanceStatusCode.LateIn:
                    return "T";

                case AttendanceStatusCode.EarlyOut:
                    return "S";

                case AttendanceStatusCode.MissingIn:
                    return "I";

                case AttendanceStatusCode.MissingOut:
                    return "O";

                case AttendanceStatusCode.NoData:
                    return "-";

                case AttendanceStatusCode.Off:
                    return "N";

                case AttendanceStatusCode.Holiday:
                    return "L";

                case AttendanceStatusCode.WorkOnOffDay:
                    return "+";

                default:
                    return "";
            }
        }

        public static bool IsIssue(string statusCode)
        {
            return statusCode == AttendanceStatusCode.LateIn
                || statusCode == AttendanceStatusCode.EarlyOut
                || statusCode == AttendanceStatusCode.MissingIn
                || statusCode == AttendanceStatusCode.MissingOut
                || statusCode == AttendanceStatusCode.NoData
                || statusCode == AttendanceStatusCode.WorkOnOffDay;
        }

        public static bool IsWorkingStatus(string statusCode)
        {
            return statusCode == AttendanceStatusCode.Ok
                || statusCode == AttendanceStatusCode.LateIn
                || statusCode == AttendanceStatusCode.EarlyOut
                || statusCode == AttendanceStatusCode.WorkOnOffDay;
        }

    }
}