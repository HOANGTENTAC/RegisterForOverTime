using System;
using System.Data;
using System.Data.SqlClient;
using static HRMS.Common.Enums;

namespace HRMS.Helpers
{
    public class GenerateHelper
    {
        public static string GenerateRequestNo(int ticketTypeId)
        {
            string prefix = string.Empty;
            string period = DateTime.Now.ToString("yyMM");
            int running = 1;

            string sql = @"SELECT COUNT(*) as Count FROM tbl_Tickets a
                INNER JOIN [TIME_KEEPING].[dbo].[mst_TicketTypes] b on a.TicketTypeId = b.TicketTypeId
                WHERE a.TicketTypeId = @TicketTypeId
                AND YEAR(a.CreatedDate)=YEAR(GETDATE())
                AND MONTH(a.CreatedDate)=MONTH(GETDATE())";

            DataTable dt = SQLHelper.ExecuteDt(sql, new SqlParameter("@TicketTypeId", ticketTypeId));
            if (dt.Rows.Count != 0)
            {
                running = int.Parse(dt.Rows[0]["Count"].ToString()) + 1;
            }

            if (Enum.IsDefined(typeof(RequestTypeEnum), ticketTypeId))
            {
                prefix = ((RequestTypeEnum)ticketTypeId).ToString();
            }

            return $"{prefix}{period}-{running:0000}";
        }
    }
}