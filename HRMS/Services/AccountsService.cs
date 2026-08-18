using HRMS.Helpers;
using HRMS.Models;
using HRMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace HRMS.Services
{
    public class AccountsService
    {
        private readonly ApplicationDbContext _context;

        private const string DefaultPassword = "Tent@c";

        public AccountsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AccountUserViewModel> LoadUsers(string keyword, string dept, int? accessLevel, string status)
        {
            string sql = @"
                SELECT nv.MaNhanVien, nv.TenNhanVien, nv.MaPhongBan, pb.TenPhongBan,
                    ISNULL(u.YeuCauCapLaiMatKhau, 0) AS YeuCauCapLaiMatKhau,
                    CASE 
                        WHEN u.MaNhanVien IS NULL THEN 0
                        ELSE 1
                    END AS HasAccount,
                    MAX(ur.AccessLevel) AS HighestAccessLevel,
                    COUNT(ur.BoPhanQuanLy) AS ManagedDepartmentsCount,
                    STUFF((
                        SELECT ', ' + ISNULL(pb2.TenPhongBan, ur2.BoPhanQuanLy)
                        FROM [TIME_KEEPING].[dbo].[UserRoles] ur2
                        LEFT JOIN [MITACOSQL].[dbo].[PHONGBAN] pb2
                            ON ur2.BoPhanQuanLy = pb2.MaPhongBan
                        WHERE ur2.MaNhanVien = nv.MaNhanVien
                        FOR XML PATH(''), TYPE
                    ).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS ManagedDepartmentsText,
                    MAX(u.NgayCapNhat) AS NgayCapNhat
                FROM [MITACOSQL].[dbo].[NHANVIEN] nv
                LEFT JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON nv.MaPhongBan = pb.MaPhongBan
                LEFT JOIN [TIME_KEEPING].[dbo].[Users] u ON nv.MaNhanVien = u.MaNhanVien
                LEFT JOIN [TIME_KEEPING].[dbo].[UserRoles] ur ON nv.MaNhanVien = ur.MaNhanVien
                WHERE (@Dept IS NULL OR nv.MaPhongBan = @Dept)
                  AND (
                        @Keyword IS NULL
                        OR nv.MaNhanVien LIKE '%' + @Keyword + '%'
                        OR nv.TenNhanVien LIKE N'%' + @Keyword + N'%'
                        OR CONVERT(varchar(20), nv.MaChamCong) LIKE '%' + @Keyword + '%'
                      )
                GROUP BY nv.MaNhanVien, nv.TenNhanVien, nv.MaPhongBan, pb.TenPhongBan, u.MaNhanVien, u.YeuCauCapLaiMatKhau
                ORDER BY pb.TenPhongBan, nv.MaNhanVien;
            ";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@Dept", string.IsNullOrEmpty(dept) ? (object)DBNull.Value : dept),
                new SqlParameter("@Keyword", string.IsNullOrEmpty(keyword) ? (object)DBNull.Value : keyword));

            var users = new List<AccountUserViewModel>();

            foreach (DataRow row in dt.Rows)
            {
                bool hasAccount = GetBool(row, "HasAccount");
                int? highestAccessLevel = GetNullableInt(row, "HighestAccessLevel");

                var item = new AccountUserViewModel
                {
                    EmployeeCD = GetString(row, "MaNhanVien"),
                    TenNhanVien = GetString(row, "TenNhanVien"),
                    MaPhongBan = GetString(row, "MaPhongBan"),
                    TenPhongBan = GetString(row, "TenPhongBan"),
                    HasAccount = hasAccount,
                    HighestAccessLevel = highestAccessLevel,
                    HighestAccessLevelName = AccessLevelHelper.GetName(highestAccessLevel),
                    ManagedDepartmentsCount = GetInt(row, "ManagedDepartmentsCount"),
                    ManagedDepartmentsText = GetString(row, "ManagedDepartmentsText"),
                    NgayCapNhat = GetDate(row, "NgayCapNhat"),
                    YeuCauCapLaiMatKhau = GetBool(row, "YeuCauCapLaiMatKhau"),
                    TrangThai = hasAccount ? "Đã tạo tài khoản" : "Chưa có tài khoản"
                };

                users.Add(item);
            }

            if (accessLevel.HasValue)
            {
                users = users
                    .Where(x => x.HighestAccessLevel == accessLevel.Value)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "has-account")
                {
                    users = users.Where(x => x.HasAccount).ToList();
                }
                else if (status == "no-account")
                {
                    users = users.Where(x => !x.HasAccount).ToList();
                }
                else if (status == "reset-request")
                {
                    users = users.Where(x => x.YeuCauCapLaiMatKhau).ToList();
                }
            }

            return users;
        }

        public List<AccountRoleViewModel> LoadRoles(string employeeCd)
        {
            if (string.IsNullOrEmpty(employeeCd))
            {
                return new List<AccountRoleViewModel>();
            }

            string sql = @"SELECT ur.MaNhanVien, nv.TenNhanVien, ur.BoPhanQuanLy, pb.TenPhongBan AS TenBoPhanQuanLy,
                    ur.AccessLevel, ur.NgayCapNhat FROM [TIME_KEEPING].[dbo].[UserRoles] ur
                LEFT JOIN [MITACOSQL].[dbo].[NHANVIEN] nv ON ur.MaNhanVien = nv.MaNhanVien
                LEFT JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON ur.BoPhanQuanLy = pb.MaPhongBan
                WHERE ur.MaNhanVien = @MaNhanVien
                ORDER BY ur.AccessLevel DESC, pb.TenPhongBan;";

            DataTable dt = SQLHelper.ExecuteDt(sql, new SqlParameter("@MaNhanVien", employeeCd)
            );

            var roles = new List<AccountRoleViewModel>();

            foreach (DataRow row in dt.Rows)
            {
                int accessLevel = GetInt(row, "AccessLevel");

                roles.Add(new AccountRoleViewModel
                {
                    EmployeeCD = GetString(row, "MaNhanVien"),
                    TenNhanVien = GetString(row, "TenNhanVien"),
                    BoPhanQuanLy = GetString(row, "BoPhanQuanLy"),
                    TenBoPhanQuanLy = GetString(row, "TenBoPhanQuanLy"),
                    AccessLevel = accessLevel,
                    AccessLevelName = AccessLevelHelper.GetName(accessLevel),
                    NgayCapNhat = GetDate(row, "NgayCapNhat")
                });
            }

            return roles;
        }

        public ServiceResult SaveRole(SaveAccountRoleRequest request)
        {
            if (request == null)
            {
                return Fail("Dữ liệu không hợp lệ.");
            }

            if (string.IsNullOrEmpty(request.EmployeeCD))
            {
                return Fail("Vui lòng chọn nhân viên.");
            }

            if (string.IsNullOrEmpty(request.BoPhanQuanLy))
            {
                return Fail("Vui lòng chọn bộ phận quản lý.");
            }

            if (request.AccessLevel <= 0)
            {
                return Fail("Vui lòng chọn cấp quyền.");
            }

            if (!EmployeeExists(request.EmployeeCD))
            {
                return Fail("Mã nhân viên không tồn tại.");
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    EnsureUserExists(request.EmployeeCD);

                    var role = _context.UserRoles.FirstOrDefault(x =>
                        x.MaNhanVien == request.EmployeeCD &&
                        x.BoPhanQuanLy == request.BoPhanQuanLy);

                    if (role == null)
                    {
                        role = new UserRolesModel
                        {
                            MaNhanVien = request.EmployeeCD,
                            BoPhanQuanLy = request.BoPhanQuanLy,
                            AccessLevel = request.AccessLevel,
                            NgayCapNhat = DateTime.Now
                        };

                        _context.UserRoles.Add(role);
                    }
                    else
                    {
                        role.AccessLevel = request.AccessLevel;
                        role.NgayCapNhat = DateTime.Now;
                    }

                    //Task: Update user admin flag after deleting the role
                    //UpdateUserAdminFlag(request.EmployeeCD);

                    _context.SaveChanges();
                    tran.Commit();

                    return Ok("Đã lưu phân quyền thành công.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        public ServiceResult DeleteRole(string employeeCd, string dept)
        {
            if (string.IsNullOrEmpty(employeeCd) || string.IsNullOrEmpty(dept))
            {
                return Fail("Thiếu thông tin quyền cần xóa.");
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var role = _context.UserRoles.FirstOrDefault(x =>
                        x.MaNhanVien == employeeCd &&
                        x.BoPhanQuanLy == dept);

                    if (role == null)
                    {
                        return Fail("Không tìm thấy quyền cần xóa.");
                    }

                    _context.UserRoles.Remove(role);

                    //Task: Update user admin flag after deleting the role
                    //UpdateUserAdminFlag(employeeCd);

                    _context.SaveChanges();
                    tran.Commit();

                    return Ok("Đã xóa quyền thành công.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        public ServiceResult ResetPassword(ResetPasswordRequest request)
        {
            if (request == null)
            {
                return Fail("Dữ liệu không hợp lệ.");
            }

            if (string.IsNullOrEmpty(request.EmployeeCD))
            {
                return Fail("Vui lòng chọn nhân viên.");
            }

            if (string.IsNullOrEmpty(request.NewPassword))
            {
                return Fail("Vui lòng nhập mật khẩu mới.");
            }

            if (!EmployeeExists(request.EmployeeCD))
            {
                return Fail("Mã nhân viên không tồn tại.");
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var user = _context.Users.FirstOrDefault(x =>
                        x.MaNhanVien == request.EmployeeCD);

                    string hashedPassword = PasswordHelper.HashPassword(request.NewPassword.Trim());

                    if (user == null)
                    {
                        user = new UsersModel
                        {
                            MaNhanVien = request.EmployeeCD,
                            MatKhau = hashedPassword,
                            YeuCauCapLaiMatKhau = false,
                            IsAdmin = false,
                            NgayCapNhat = DateTime.Now
                        };

                        _context.Users.Add(user);
                    }
                    else
                    {
                        user.YeuCauCapLaiMatKhau = false;
                        user.MatKhau = hashedPassword;
                        user.NgayCapNhat = DateTime.Now;
                    }

                    _context.SaveChanges();
                    tran.Commit();

                    return Ok("Đã reset mật khẩu thành công.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        public ServiceResult CreateAccount(string employeeCd, string password = null)
        {
            if (string.IsNullOrEmpty(employeeCd))
            {
                return Fail("Vui lòng chọn nhân viên.");
            }

            if (!EmployeeExists(employeeCd))
            {
                return Fail("Mã nhân viên không tồn tại.");
            }

            using (var tran = _context.Database.BeginTransaction())
            {
                try
                {
                    var user = _context.Users.FirstOrDefault(x =>
                        x.MaNhanVien == employeeCd);

                    if (user != null)
                    {
                        return Fail("Tài khoản đã tồn tại.");
                    }

                    user = new UsersModel
                    {
                        MaNhanVien = employeeCd,
                        MatKhau = PasswordHelper.HashPassword(string.IsNullOrEmpty(password) ? DefaultPassword : password.Trim()),
                        IsAdmin = false,
                        YeuCauCapLaiMatKhau = false,
                        NgayCapNhat = DateTime.Now
                    };

                    _context.Users.Add(user);
                    _context.SaveChanges();

                    tran.Commit();

                    return Ok("Đã tạo tài khoản thành công.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Fail(ex.Message);
                }
            }
        }

        private void EnsureUserExists(string employeeCd)
        {
            var user = _context.Users.FirstOrDefault(x =>
                x.MaNhanVien == employeeCd);

            if (user != null)
            {
                return;
            }

            user = new UsersModel
            {
                MaNhanVien = employeeCd,
                MatKhau = PasswordHelper.HashPassword(DefaultPassword),
                YeuCauCapLaiMatKhau = false,
                IsAdmin = false,
                NgayCapNhat = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();
        }

        private void UpdateUserAdminFlag(string employeeCd)
        {
            var user = _context.Users.FirstOrDefault(x =>
                x.MaNhanVien == employeeCd);

            if (user == null)
            {
                return;
            }

            bool isAdmin = _context.UserRoles.Any(x =>
                x.MaNhanVien == employeeCd &&
                x.AccessLevel == 5);

            user.IsAdmin = isAdmin;
            user.NgayCapNhat = DateTime.Now;
        }

        private bool EmployeeExists(string employeeCd)
        {
            string sql = @" SELECT COUNT(1)
                FROM [MITACOSQL].[dbo].[NHANVIEN]
                WHERE MaNhanVien = @MaNhanVien;";

            object result = SQLHelper.ExecuteScalar(sql,
                new SqlParameter("@MaNhanVien", employeeCd)
            );

            return Convert.ToInt32(result) > 0;
        }

        private ServiceResult Ok(string message)
        {
            return new ServiceResult
            {
                Success = true,
                Message = message
            };
        }

        private ServiceResult Fail(string message)
        {
            return new ServiceResult
            {
                Success = false,
                Message = message
            };
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

        private int? GetNullableInt(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) ||
                row[columnName] == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(row[columnName]);
        }

        private bool GetBool(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) ||
                row[columnName] == DBNull.Value)
            {
                return false;
            }

            return Convert.ToBoolean(row[columnName]);
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