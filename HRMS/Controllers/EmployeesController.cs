using HRMS.Helpers;
using HRMS.Models;
using HRMS.Services;
using HRMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HRMS.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly PermissionService _permissionService;

        public EmployeesController()
        {
            _db = new ApplicationDbContext();
            _permissionService = new PermissionService();
        }

        public ActionResult Index(string keyword, string dept, string status)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var currentUser = Session["LoginInfo"] as UsersModel;

            if (!_permissionService.CanViewEmployeeList(currentUser))
            {
                return RedirectToAction("Detail", "Employees",
                    new
                    {
                        employeeCd = currentUser.MaNhanVien
                    });
            }

            var employees = LoadEmployees(keyword, dept, status, currentUser);

            var model = new EmployeePageViewModel
            {
                Keyword = keyword,
                SelectedDept = dept,
                SelectedStatus = status,
                Departments = PermissionScopeHelper.LoadDepartmentsForUser(currentUser, _db, _permissionService),
                Employees = employees,

                TotalEmployees = employees.Count,
                TotalDepartments = employees
                    .Where(x => !string.IsNullOrEmpty(x.MaPhongBan))
                    .Select(x => x.MaPhongBan)
                    .Distinct()
                    .Count(),
                TotalNewEmployees = employees.Count(x => x.NhanVienMoi),
                TotalInsurance = employees.Count(x => x.DangThamGiaBaoHiem)
            };

            return View(model);
        }

        [HttpGet]
        public JsonResult Data(string keyword, string dept, string status)
        {
            try
            {
                if (Session["LoginInfo"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn."
                    }, JsonRequestBehavior.AllowGet);
                }

                var currentUser = Session["LoginInfo"] as UsersModel;

                if (!_permissionService.CanViewEmployeeList(currentUser))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bạn không có quyền xem danh sách nhân viên."
                    }, JsonRequestBehavior.AllowGet);
                }

                var employees = LoadEmployees(keyword, dept, status, currentUser);

                return Json(new
                {
                    success = true,

                    summary = new
                    {
                        totalEmployees = employees.Count,
                        totalDepartments = employees
                            .Where(x => !string.IsNullOrEmpty(x.MaPhongBan))
                            .Select(x => x.MaPhongBan)
                            .Distinct()
                            .Count(),
                        totalNewEmployees = employees.Count(x => x.NhanVienMoi),
                        totalInsurance = employees.Count(x => x.DangThamGiaBaoHiem)
                    },

                    rows = employees.Select(x => new
                    {
                        x.EmployeeCD,
                        x.TenNhanVien,
                        x.MaChamCong,
                        x.MaThe,
                        x.MaPhongBan,
                        x.TenPhongBan,
                        x.ChucVu,
                        NgayVaoLamViec = x.NgayVaoLamViec.HasValue
                            ? x.NgayVaoLamViec.Value.ToString("dd/MM/yyyy")
                            : "",
                        x.DangThamGiaBaoHiem,
                        x.NhanVienMoi,
                        x.NghiViecTamThoi,
                        x.UserEnable,
                        x.TrangThai
                    })
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Detail(string employeeCd)
        {
            if (Session["LoginInfo"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(employeeCd))
            {
                return RedirectToAction("Index");
            }

            var currentUser = Session["LoginInfo"] as UsersModel;

            if (!_permissionService.CanViewEmployeeDetail(currentUser, employeeCd))
            {
                TempData["ToastrType"] = "error";
                TempData["ToastrMessage"] = "Bạn không có quyền xem hồ sơ nhân viên này.";

                return RedirectToAction("Index", "Home");
            }

            var employee = LoadEmployeeProfile(employeeCd);

            if (employee == null)
            {
                return HttpNotFound();
            }

            return View(employee);
        }

        private List<EmployeeListItemViewModel> LoadEmployees(string keyword, string dept, string status, UsersModel currentUser)
        {
            var parameters = new List<SqlParameter>{
                new SqlParameter("@Dept", string.IsNullOrEmpty(dept) ? (object)DBNull.Value : dept),
                new SqlParameter("@Keyword", string.IsNullOrEmpty(keyword) ? (object)DBNull.Value : keyword)};

            string permissionWhere = PermissionScopeHelper.BuildEmployeeScopeWhere(currentUser, _permissionService, dept,
                "nv", "nv.MaPhongBan", parameters);

            if (permissionWhere == "NO_ACCESS")
            {
                return new List<EmployeeListItemViewModel>();
            }

            string sql = @"SELECT nv.MaNhanVien, nv.TenNhanVien, nv.MaChamCong, nv.MaThe, nv.MaPhongBan, pb.TenPhongBan,
            nv.NgayVaoLamViec, nv.DangThamGiaBaoHiem, nv.NhanVienMoi, nv.NghiViecTamThoi, nv.UserEnable 
            FROM [MITACOSQL].[dbo].NHANVIEN nv
            LEFT JOIN PhongBan pb ON nv.MaPhongBan = pb.MaPhongBan
            WHERE (@Dept IS NULL OR nv.MaPhongBan = @Dept)
              AND (
                    @Keyword IS NULL
                    OR nv.MaNhanVien LIKE '%' + @Keyword + '%'
                    OR nv.TenNhanVien LIKE N'%' + @Keyword + N'%'
                    OR CONVERT(varchar(20), nv.MaChamCong) LIKE '%' + @Keyword + '%'
                    OR nv.MaThe LIKE '%' + @Keyword + '%'
                  )
            ";
            sql += permissionWhere;

            sql += @"ORDER BY pb.TenPhongBan, nv.MaNhanVien";

            DataTable dt = SQLHelper.ExecuteDt(sql, parameters.ToArray());

            var list = new List<EmployeeListItemViewModel>();

            foreach (DataRow row in dt.Rows)
            {
                bool nghiTamThoi = GetBool(row, "NghiViecTamThoi");
                bool nhanVienMoi = GetBool(row, "NhanVienMoi");
                bool baoHiem = GetBool(row, "DangThamGiaBaoHiem");
                string userEnable = GetString(row, "UserEnable");

                var item = new EmployeeListItemViewModel
                {
                    EmployeeCD = GetString(row, "MaNhanVien"),
                    TenNhanVien = GetString(row, "TenNhanVien"),
                    MaChamCong = GetInt(row, "MaChamCong"),
                    MaThe = GetString(row, "MaThe"),
                    MaPhongBan = GetString(row, "MaPhongBan"),
                    TenPhongBan = GetString(row, "TenPhongBan"),
                    NgayVaoLamViec = GetDate(row, "NgayVaoLamViec"),
                    DangThamGiaBaoHiem = baoHiem,
                    NhanVienMoi = nhanVienMoi,
                    NghiViecTamThoi = nghiTamThoi,
                    UserEnable = userEnable
                };

                item.TrangThai = BuildEmployeeStatus(item.UserEnable, item.NghiViecTamThoi);

                list.Add(item);
            }

            if (!string.IsNullOrEmpty(status))
            {
                list = list
                    .Where(x => string.Equals(x.TrangThai, status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return list;
        }

        private EmployeeProfileViewModel LoadEmployeeProfile(string employeeCd)
        {
            string sql = @"
                SELECT nv.MaNhanVien, nv.TenNhanVien, nv.MaChamCong, nv.TenChamCong, nv.MaThe, nv.GioiTinh,
                    nv.NgayVaoLamViec, nv.NgaySinh, nv.NoiSinh, nv.NgayKyHopDong, nv.ThoiHanHopDong, nv.CMND, 
                    nv.NgayCap, nv.NoiCap, nv.DienThoaiLienHe, nv.Email, nv.DanToc, nv.QuocTich, 
                    CASE 
                        WHEN nv.HinhAnh IS NOT NULL AND DATALENGTH(nv.HinhAnh) > 0 
                        THEN 1 
                        ELSE 0 
                    END AS HasPhoto,
                    nv.TrinhDo, nv.MaCongTy, nv.MaKhuVuc, nv.MaPhongBan, pb.TenPhongBan, nv.DangThamGiaBaoHiem, 
                    nv.NghiViecTamThoi, nv.NhanVienMoi, nv.GhiChu, nv.UserEnable
                FROM [MITACOSQL].[dbo].NHANVIEN nv
                LEFT JOIN PhongBan pb ON nv.MaPhongBan = pb.MaPhongBan
                WHERE nv.MaNhanVien = @EmployeeCD";

            DataTable dt = SQLHelper.ExecuteDt(sql,
                new SqlParameter("@EmployeeCD", employeeCd));

            if (dt.Rows.Count == 0)
            {
                return null;
            }

            DataRow row = dt.Rows[0];

            bool nghiTamThoi = GetBool(row, "NghiViecTamThoi");
            string userEnable = GetString(row, "UserEnable");

            return new EmployeeProfileViewModel
            {
                EmployeeCD = GetString(row, "MaNhanVien"),
                TenNhanVien = GetString(row, "TenNhanVien"),
                MaChamCong = GetInt(row, "MaChamCong"),
                TenChamCong = GetString(row, "TenChamCong"),
                MaThe = GetString(row, "MaThe"),
                GioiTinh = GetBool(row, "GioiTinh"),
                NgayVaoLamViec = GetDate(row, "NgayVaoLamViec"),
                NgaySinh = GetDate(row, "NgaySinh"),
                NoiSinh = GetString(row, "NoiSinh"),
                NgayKyHopDong = GetDate(row, "NgayKyHopDong"),
                ThoiHanHopDong = GetFloat(row, "ThoiHanHopDong"),
                CMND = GetString(row, "CMND"),
                NgayCap = GetDate(row, "NgayCap"),
                NoiCap = GetString(row, "NoiCap"),
                DienThoaiLienHe = GetString(row, "DienThoaiLienHe"),
                Email = GetString(row, "Email"),
                NgayPhep = GetFloat(row, "NgayPhep"),
                DanToc = GetString(row, "DanToc"),
                QuocTich = GetString(row, "QuocTich"),
                TrinhDo = GetString(row, "TrinhDo"),
                MaCongTy = GetString(row, "MaCongTy"),
                MaKhuVuc = GetString(row, "MaKhuVuc"),
                MaPhongBan = GetString(row, "MaPhongBan"),
                TenPhongBan = GetString(row, "TenPhongBan"),
                DangThamGiaBaoHiem = GetBool(row, "DangThamGiaBaoHiem"),
                NghiViecTamThoi = nghiTamThoi,
                NhanVienMoi = GetBool(row, "NhanVienMoi"),
                HasPhoto = GetBool(row, "HasPhoto"),
                GhiChu = GetString(row, "GhiChu"),
                UserEnable = userEnable,
                TrangThai = BuildEmployeeStatus(userEnable, nghiTamThoi)
            };
        }

        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0)]
        public ActionResult GetPhoto(string employeeCd)
        {
            if (string.IsNullOrWhiteSpace(employeeCd))
            {
                return HttpNotFound("Mã nhân viên rỗng");
            }

            string cleanCd = employeeCd.Trim();
            string sql = "SELECT HinhAnh FROM [MITACOSQL].[dbo].NHANVIEN WHERE RTRIM(LTRIM(MaNhanVien)) = @EmployeeCD";

            DataTable dt = SQLHelper.ExecuteDt(sql, new SqlParameter("@EmployeeCD", cleanCd));

            if (dt == null || dt.Rows.Count == 0 || dt.Rows[0]["HinhAnh"] == DBNull.Value)
            {
                return HttpNotFound("Không tìm thấy dữ liệu trong DB");
            }

            object rawValue = dt.Rows[0]["HinhAnh"];
            byte[] bytes = null;

            // Trường hợp 1: Cột SQL là varbinary/image (Binary byte[])
            if (rawValue is byte[] rawBytes)
            {
                bytes = rawBytes;
            }
            // Trường hợp 2: Cột SQL là varchar/nvarchar (Chuỗi Hex "0xFFD8..." hoặc "FFD8...")
            else if (rawValue is string hexString)
            {
                bytes = ConvertHexStringToByteArray(hexString);
            }

            if (bytes == null || bytes.Length == 0)
            {
                return HttpNotFound("Mảng byte sau chuyển đổi bị rỗng");
            }

            // Tự động dò vị trí byte ảnh thực sự (tránh OLE Header nếu có)
            byte[] cleanBytes = ExtractValidImageBytes(bytes);
            string mimeType = GetImageMimeType(cleanBytes);

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            return File(cleanBytes, mimeType);
        }

        /// <summary>
        /// Chuyển đổi chuỗi Hex (VD: 0xFFD8... hoặc FFD8...) thành mảng byte[]
        /// </summary>
        private byte[] ConvertHexStringToByteArray(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;

            hex = hex.Trim();
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }

            if (hex.Length % 2 != 0) return null;

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        private byte[] ExtractValidImageBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4)
                return bytes;

            for (int i = 0; i <= bytes.Length - 4; i++)
            {
                // 1. Check JPEG Magic Bytes: FF D8 FF
                if (bytes[i] == 0xFF && bytes[i + 1] == 0xD8 && bytes[i + 2] == 0xFF)
                {
                    if (i == 0)
                    {
                        return bytes;
                    }
                    byte[] dest = new byte[bytes.Length - i];
                    Array.Copy(bytes, i, dest, 0, dest.Length);
                    return dest;
                }

                // 2. Check PNG Magic Bytes: 89 50 4E 47
                if (bytes[i] == 0x89 && bytes[i + 1] == 0x50 && bytes[i + 2] == 0x4E && bytes[i + 3] == 0x47)
                {
                    if (i == 0)
                    {
                        return bytes;
                    }
                    byte[] dest = new byte[bytes.Length - i];
                    Array.Copy(bytes, i, dest, 0, dest.Length);
                    return dest;
                }
            }
            return bytes;
        }

        private string GetImageMimeType(byte[] bytes)
        {
            if (bytes != null && bytes.Length >= 4)
            {
                // PNG Header
                if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                {
                    return "image/png";
                }
                // JPEG Header
                if (bytes[0] == 0xFF && bytes[1] == 0xD8)
                {
                    return "image/jpeg";
                }
            }
            return "image/jpeg";
        }

        private string BuildEmployeeStatus(string userEnable, bool nghiViecTamThoi)
        {
            if (nghiViecTamThoi)
            {
                return "Tạm nghỉ";
            }

            if (string.IsNullOrEmpty(userEnable))
            {
                return "Đang làm việc";
            }

            string normalized = userEnable.Trim().ToLower();

            if (normalized == "1" || normalized == "true" || normalized == "yes" || normalized == "y")
            {
                return "Đang làm việc";
            }

            return string.Empty;
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

        private float GetFloat(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) ||
                row[columnName] == DBNull.Value)
            {
                return 0;
            }

            return Convert.ToSingle(row[columnName]);
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