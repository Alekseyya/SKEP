using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers;

namespace MainApp.Helpers
{
    //Todo надо будет убрать
    public class GenerateAnchorService : IGenerateAnchorService
    {
        
        private readonly IUrlHelper _urlHelper;
        public GenerateAnchorService(IUrlHelper urlHelper)
        {
            _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
        }

        public string GenerateAnchor(string linkText, string action, object routeValues = null)
        {
            var newUrlHelper = new UrlHelper(_urlHelper.ActionContext);
            var anchor = new TagBuilder("a");
            anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);

            if (routeValues != null)
                anchor.Attributes["href"] = newUrlHelper.Action(action, routeValues);
            else
                anchor.Attributes["href"] = newUrlHelper.Action(action);

            return anchor.ToString();
        }
    }
}
