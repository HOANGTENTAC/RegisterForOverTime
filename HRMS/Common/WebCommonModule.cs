using HRMS.Helpers;
using HRMS.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace HRMS.Common
{
    public class WebCommonModule
    {
        public UsersModel GetUser(string maNhanVien)
        {
            string sql = @"SELECT nv.MaNhanVien, nv.MaChamCong, TenNhanVien, nv.MaPhongBan, pb.TenPhongBan, HinhAnh, 
                u.MatKhau, u.isAdmin
                FROM [MITACOSQL].[dbo].NHANVIEN nv
                LEFT JOIN Users u ON nv.MaNhanVien = u.MaNhanVien
                INNER JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON nv.MaPhongBan = pb.MaPhongBan
                WHERE nv.MaNhanVien = @MaNhanVien";

            DataTable dt = SQLHelper.ExecuteDt(sql, new SqlParameter("@MaNhanVien", maNhanVien.ToUpper()));

            if (dt.Rows.Count == 0) return null;

            var model = new UsersModel
            {
                MaNhanVien = dt.Rows[0]["MaNhanVien"].ToString(),
                MaChamCong = int.Parse(dt.Rows[0]["MaChamCong"].ToString()),
                TenNhanVien = dt.Rows[0]["TenNhanVien"].ToString(),
                MaPhongBan = dt.Rows[0]["MaPhongBan"].ToString(),
                TenPhongBan = dt.Rows[0]["TenPhongBan"].ToString(),
                IsAdmin = dt.Rows[0]["isAdmin"].Equals(true),
                MatKhau = dt.Rows[0]["MatKhau"].ToString()
            };

            // Avatar: convert từ byte[] sang base64
            using (var ms = new MemoryStream((byte[])dt.Rows[0]["HinhAnh"]))
            using (var img = Image.FromStream(ms))
            {
                var thumb = new Bitmap(img, new Size(36, 36));
                using (var msThumb = new MemoryStream())
                {
                    thumb.Save(msThumb, ImageFormat.Png);
                    string base64 = Convert.ToBase64String(msThumb.ToArray());
                    model.Avatar = "data:image/png;base64," + base64;
                }
            }

            return model;
        }

        public List<UserRolesModel> GetConfirmUserCD()
        {
            string sql = @"SELECT distinct(UserRoles.MaNhanVien), nv.TenNhanVien, nv.MaPhongBan, pb.TenPhongBan FROM UserRoles
            INNER JOIN [MITACOSQL].[dbo].[NHANVIEN] nv ON nv.MaNhanVien = UserRoles.MaNhanVien
            INNER JOIN [MITACOSQL].[dbo].[PHONGBAN] pb ON nv.MaPhongBan = pb.MaPhongBan
            WHERE AccessLevel = 3 OR  AccessLevel = 5 AND UserRoles.MaNhanVien != 'OF1094'";
            DataTable dt = SQLHelper.ExecuteDt(sql);
            List<UserRolesModel> userRoles = new List<UserRolesModel>();
            foreach (DataRow row in dt.Rows)
            {
                userRoles.Add(new UserRolesModel
                {
                    MaNhanVien = row["MaNhanVien"].ToString(),
                    TenNhanVien = row["TenNhanVien"].ToString(),
                    BoPhanQuanLy = row["MaPhongBan"].ToString(),
                    TenPhongBan = row["TenPhongBan"].ToString(),
                });
            }
            return userRoles;
        }
    }
}