namespace HRMS.Common
{
    public class DBCommon
    {
        public const string pDEF_SERVER = @"192.168.40.253";
        public const string pDEF_DATABASE = "TIME_KEEPING";
        public const string pDEF_USER_ID = "sa";
        public const string pDEF_PASSWORD = "Seit0n@2k16";
        public static string ConnectionString()
        {
            string connectStr = $"Server={pDEF_SERVER};" +
                                $"Database={pDEF_DATABASE};" +
                                $"User Id={pDEF_USER_ID};" +
                                $"Password={pDEF_PASSWORD};" +
                                $"Connection Timeout=3600;";
            return connectStr;
        }
    }
}