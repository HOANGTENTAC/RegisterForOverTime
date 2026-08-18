using System.Collections.Generic;
using System.Web.Mvc;

namespace HRMS.Helpers
{
    public static class AccessLevelHelper
    {
        public static string GetName(int? level)
        {
            if (!level.HasValue)
            {
                return "Chưa phân quyền";
            }

            switch (level.Value)
            {
                case 1:
                    return "Nhân viên";
                case 2:
                    return "Trưởng nhóm";
                case 3:
                    return "Quản lý";
                case 4:
                    return "Giám đốc";
                case 5:
                    return "Quản trị viên";
                default:
                    return "Không xác định";
            }
        }

        public static List<SelectListItem> GetSelectList(int? selected = null)
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Text = "Nhân viên", Value = "1" },
                new SelectListItem { Text = "Trưởng nhóm", Value = "2" },
                new SelectListItem { Text = "Quản lý", Value = "3" },
                new SelectListItem { Text = "Giám đốc", Value = "4" },
                new SelectListItem { Text = "Quản trị viên", Value = "5" }
            };

            if (selected.HasValue)
            {
                foreach (var item in items)
                {
                    item.Selected = item.Value == selected.Value.ToString();
                }
            }

            return items;
        }

        public static bool IsAdminLevel(int? level)
        {
            return level.HasValue && level.Value == 5;
        }

        public static bool IsManagerLevel(int? level)
        {
            return level.HasValue && level.Value >= 2 && level.Value <= 4;
        }

    }
}