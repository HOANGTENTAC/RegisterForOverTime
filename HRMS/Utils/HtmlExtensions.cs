using System.Web;
using System.Web.Mvc;

namespace HRMS.Utils
{
    public static class HtmlExtensions
    {
        public static IHtmlString RenderStatusBadge(this HtmlHelper html, int statusId, string statusName)
        {
            string cssClass;
            string iconClass;

            switch (statusId)
            {
                case 10:
                    cssClass = "status-pending";
                    iconClass = "fa fa-clock";
                    break;
                case 20:
                    cssClass = "status-approved";
                    iconClass = "fa fa-check-circle";
                    break;
                case 30:
                    cssClass = ".status-rejected";
                    iconClass = "fa fa-times-circle";
                    break;
                case 40:
                    cssClass = "status-rejected";
                    iconClass = "fa fa-times-circle";
                    break;
                case 90:
                    cssClass = "status-finished";
                    iconClass = "fa fa-check-circle";
                    break;
                default:
                    cssClass = "status-cancelled ";
                    iconClass = "fa fa-times-circle";
                    break;
            }

            string htmlString = $"<div class=\"status-badge {cssClass}\"><i class=\"{iconClass}\"></i> {statusName}</div>";
            return new HtmlString(htmlString);
        }
    }
}