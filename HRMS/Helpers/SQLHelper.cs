using HRMS.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace HRMS.Helpers
{
    public static class SQLHelper
    {
        //private static string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        private static string connStr = DBCommon.ConnectionString();

        // Trả về DataTable
        public static DataTable ExecuteDt(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        // Trả về số dòng ảnh hưởng
        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        // Trả về giá trị đơn (ví dụ COUNT, MAX…)
        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        // Trả về danh sách model (generic)
        public static List<T> ExecuteList<T>(string sql, Func<IDataReader, T> map, params SqlParameter[] parameters)
        {
            var list = new List<T>();
            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(map(reader));
                    }
                }
            }
            return list;
        }

        public static string GetConnectionString()
        {
            return connStr;
        }
    }
}