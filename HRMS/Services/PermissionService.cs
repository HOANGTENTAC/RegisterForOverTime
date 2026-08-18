using HRMS.Helpers;
using HRMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace HRMS.Services
{
    public class PermissionService
    {
        public bool IsSystemAdmin(UsersModel user)
        {
            return user != null && user.IsAdmin;
        }

        public List<UserRolesModel> GetUserRoles(string employeeCd)
        {
            if (string.IsNullOrEmpty(employeeCd))
            {
                return new List<UserRolesModel>();
            }

            string sql = @"SELECT MaNhanVien, BoPhanQuanLy, AccessLevel, NgayCapNhat
                FROM [TIME_KEEPING].[dbo].[UserRoles]
                WHERE MaNhanVien = @MaNhanVien";

            DataTable dt = SQLHelper.ExecuteDt(
                sql,
                new SqlParameter("@MaNhanVien", employeeCd)
            );

            var roles = new List<UserRolesModel>();

            foreach (DataRow row in dt.Rows)
            {
                roles.Add(new UserRolesModel
                {
                    MaNhanVien = GetString(row, "MaNhanVien"),
                    BoPhanQuanLy = GetString(row, "BoPhanQuanLy"),
                    AccessLevel = GetInt(row, "AccessLevel"),
                    NgayCapNhat = GetDate(row, "NgayCapNhat") ?? DateTime.Now
                });
            }

            return roles;
        }

        public bool HasAccessLevel(UsersModel user, int accessLevel)
        {
            if (user == null)
            {
                return false;
            }

            var roles = GetUserRoles(user.MaNhanVien);

            return roles.Any(x => x.AccessLevel == accessLevel);
        }

        public int GetHighestAccessLevel(string employeeCd)
        {
            var roles = GetUserRoles(employeeCd);

            if (roles.Count == 0)
            {
                return 1;
            }

            return roles.Max(x => x.AccessLevel);
        }

        public bool CanViewAllData(UsersModel user)
        {
            if (user == null)
            {
                return false;
            }

            // IT / quản trị hệ thống
            if (user.IsAdmin)
            {
                return true;
            }

            // Quản trị viên nghiệp vụ
            return HasAccessLevel(user, 5);
        }

        public bool CanOverrideTicketData(UsersModel user)
        {
            return CanViewAllData(user);
        }

        public bool CanViewEmployeeList(UsersModel currentUser)
        {
            if (currentUser == null)
            {
                return false;
            }

            if (CanViewAllData(currentUser))
            {
                return true;
            }

            var managedDepartments = GetManagedDepartmentCodes(currentUser.MaNhanVien);

            return managedDepartments.Any();
        }

        public bool CanViewEmployeeDetail(UsersModel currentUser, string targetEmployeeCd)
        {
            if (currentUser == null || string.IsNullOrEmpty(targetEmployeeCd))
            {
                return false;
            }

            // Ai cũng được xem hồ sơ của chính mình
            if (string.Equals(currentUser.MaNhanVien, targetEmployeeCd, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // isAdmin hoặc AccessLevel = 5 được xem tất cả
            if (CanViewAllData(currentUser))
            {
                return true;
            }

            return CanViewManagedEmployee(currentUser, targetEmployeeCd);
        }

        public List<string> GetManagedDepartmentCodes(string employeeCd)
        {
            return GetUserRoles(employeeCd)
                .Where(x => !string.IsNullOrEmpty(x.BoPhanQuanLy))
                .Where(x => x.AccessLevel >= 2 && x.AccessLevel <= 4)
                .Select(x => x.BoPhanQuanLy)
                .Distinct()
                .ToList();
        }

        public bool CanViewDepartmentData(UsersModel currentUser, string departmentCode)
        {
            if (currentUser == null || string.IsNullOrEmpty(departmentCode))
            {
                return false;
            }

            if (CanViewAllData(currentUser))
            {
                return true;
            }

            var departments = GetManagedDepartmentCodes(currentUser.MaNhanVien);

            return departments.Any(x => string.Equals(x, departmentCode, StringComparison.OrdinalIgnoreCase));
        }

        public bool CanViewManagedEmployee(UsersModel currentUser, string targetEmployeeCd)
        {
            if (currentUser == null || string.IsNullOrEmpty(targetEmployeeCd))
            {
                return false;
            }

            var managedDepartments = GetManagedDepartmentCodes(currentUser.MaNhanVien);

            if (managedDepartments == null || managedDepartments.Count == 0)
            {
                return false;
            }

            string sql = string.Empty;

            sql = @"SELECT MaPhongBan FROM [MITACOSQL].[dbo].[NHANVIEN] 
                WHERE MaNhanVien = @MaNhanVien";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@MaNhanVien", targetEmployeeCd));

            if (dt.Rows.Count == 0)
            {
                return false;
            }

            string targetDepartment = GetString(dt.Rows[0], "MaPhongBan");

            if (string.IsNullOrEmpty(targetDepartment))
            {
                return false;
            }

            return managedDepartments.Any(x => string.Equals(x, targetDepartment, StringComparison.OrdinalIgnoreCase));
        }

        private string GetString(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) ||
                row[columnName] == DBNull.Value)
            {
                return "";
            }

            return row[columnName].ToString();
        }

        private int GetInt(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) ||
                row[columnName] == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToInt32(row[columnName]);
        }

        private DateTime? GetDate(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) ||
                row[columnName] == DBNull.Value)
            {
                return null;
            }

            return Convert.ToDateTime(row[columnName]);
        }
    }
}