using System;
using System.Linq;

namespace HRMS.Utils
{
    public static class StringExtensions
    {
        public static string ToAvatarInitials(this string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "?";
            }

            // Xử lý cắt chuỗi lấy chữ cái đầu của Tên
            var nameParts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length == 0)
            {
                return "?";
            }

            // Lấy chữ cái đầu của từ cuối cùng và viết hoa
            return nameParts.Last().Substring(0, 1).ToUpper();
        }

        public static string Nz(string hikiSuu, string hikiSuu2 = "")
        {
            if (string.IsNullOrEmpty(hikiSuu) && hikiSuu != "0")
            {
                if (hikiSuu2 == null)
                {
                    return "";
                }
                else
                {
                    return hikiSuu2;
                }
            }
            else
            {
                return hikiSuu;
            }
        }
    }
}