using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MainApp.HtmlControls
{
    public static class HtmlControls
    {
        public static IHtmlContent Label(this IHtmlHelper html, string labelText, object htmlAttributes)
        {
            return html.Label("", labelText, htmlAttributes);
        }
    }
}
