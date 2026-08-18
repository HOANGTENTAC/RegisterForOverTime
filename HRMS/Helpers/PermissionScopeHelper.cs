using HRMS.Models;
using HRMS.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace HRMS.Helpers
{
    public static class PermissionScopeHelper
    {
        public static List<DepartmentModel> LoadDepartmentsForUser(UsersModel currentUser, ApplicationDbContext db, PermissionService permissionService)
        {
            if (currentUser == null)
            {
                return new List<DepartmentModel>();
            }

            if (permissionService.CanViewAllData(currentUser))
            {
                return db.Departments
                    .OrderBy(x => x.TenPhongBan)
                    .ToList();
            }

            var managedDepartments = permissionService.GetManagedDepartmentCodes(currentUser.MaNhanVien);

            if (managedDepartments != null && managedDepartments.Count > 0)
            {
                return db.Departments
                    .Where(x => managedDepartments.Contains(x.MaPhongBan))
                    .OrderBy(x => x.TenPhongBan)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(currentUser.MaPhongBan))
            {
                return db.Departments
                    .Where(x => x.MaPhongBan == currentUser.MaPhongBan)
                    .OrderBy(x => x.TenPhongBan)
                    .ToList();
            }

            return new List<DepartmentModel>();
        }

        public static string BuildEmployeeScopeWhere(UsersModel currentUser, PermissionService permissionService,
            string selectedDept, string employeeAlias, string departmentColumnExpression, List<SqlParameter> parameters)
        {
            if (currentUser == null)
            {
                return "NO_ACCESS";
            }

            // Admin / AccessLevel 5 xem toàn bộ
            if (permissionService.CanViewAllData(currentUser))
            {
                return "";
            }

            var managedDepartments = permissionService.GetManagedDepartmentCodes(currentUser.MaNhanVien);

            // Quản lý / Giám đốc
            if (managedDepartments != null && managedDepartments.Count > 0)
            {
                // SỬA LỖI: Thêm dấu ! (Nếu phòng chọn KHÔNG NẰM TRONG danh sách quản lý => Không có quyền)
                if (!string.IsNullOrEmpty(selectedDept) &&
                    !managedDepartments.Any(x => string.Equals(x, selectedDept, StringComparison.OrdinalIgnoreCase)))
                {
                    return "NO_ACCESS";
                }

                var deptParamNames = new List<string>();

                for (int i = 0; i < managedDepartments.Count; i++)
                {
                    string paramName = "@ScopeDept" + i;
                    deptParamNames.Add(paramName);
                    parameters.Add(new SqlParameter(paramName, managedDepartments[i]));
                }

                return $" AND {departmentColumnExpression} IN ({string.Join(",", deptParamNames)}) ";
            }

            // Nhân viên thường: Chỉ xem chính mình
            parameters.Add(new SqlParameter("@ScopeEmployeeCD", currentUser.MaNhanVien));
            return $" AND {employeeAlias}.MaNhanVien = @ScopeEmployeeCD ";
        }
    }
}