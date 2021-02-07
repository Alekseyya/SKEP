using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using Core.BL.Interfaces;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;



namespace MainApp.HtmlControls
{
    
    ////TODO по хорошему посмотреть исходники AnchorTagHelper
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "linkText")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-action")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-controller")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-area")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-page")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-page-handler")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-fragment")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-host")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-protocol")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-route")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-all-route-data")]
    //[HtmlTargetElement("ActionLinkWithPermission", Attributes = "asp-route-*")]
    //public class ActionLinkWithPermissionTagHelper : TagHelper
    //{
    //    private const string ActionAttributeName = "asp-action";
    //    private const string ControllerAttributeName = "asp-controller";
    //    private const string AreaAttributeName = "asp-area";
    //    private const string PageAttributeName = "asp-page";
    //    private const string PageHandlerAttributeName = "asp-page-handler";
    //    private const string FragmentAttributeName = "asp-fragment";
    //    private const string HostAttributeName = "asp-host";
    //    private const string ProtocolAttributeName = "asp-protocol";
    //    private const string RouteAttributeName = "asp-route";
    //    private const string RouteValuesDictionaryName = "asp-all-route-data";
    //    private const string RouteValuesPrefix = "asp-route-";
    //    private const string Href = "href";
    //    private IDictionary<string, string> _routeValues;

    //    protected IHtmlGenerator Generator { get; }

    //    [HtmlAttributeName(ActionAttributeName)]
    //    public string Action { get; set; }

    //    [HtmlAttributeName(ControllerAttributeName)]
    //    public string Controller { get; set; }

    //    [HtmlAttributeName(AreaAttributeName)]
    //    public string Area { get; set; }

    //    [HtmlAttributeName(PageAttributeName)]
    //    public string Page { get; set; }

    //    [HtmlAttributeName(PageHandlerAttributeName)]
    //    public string PageHandler { get; set; }
        
    //    [HtmlAttributeName(ProtocolAttributeName)]
    //    public string Protocol { get; set; }

    //    [HtmlAttributeName(HostAttributeName)]
    //    public string Host { get; set; }

    //    [HtmlAttributeName(FragmentAttributeName)]
    //    public string Fragment { get; set; }

    //    [HtmlAttributeName(RouteAttributeName)]
    //    public string Route { get; set; }

    //    [HtmlAttributeName("linkText")]
    //    public string LinkText { get; set; }

    //    [HtmlAttributeName(RouteValuesDictionaryName, DictionaryAttributePrefix = RouteValuesPrefix)]
    //    public IDictionary<string, string> RouteValues
    //    {
    //        get
    //        {
    //            if (this._routeValues == null)
    //                this._routeValues =
    //                    (IDictionary<string, string>)new Dictionary<string, string>(
    //                        (IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
    //            return this._routeValues;
    //        }
    //        set { this._routeValues = value; }
    //    }


    //    [HtmlAttributeNotBound]
    //    [ViewContext]
    //    public ViewContext ViewContext { get; set; }

    //    public OperationSet OperationSet { get; set; }

    //    private readonly IApplicationUserService _applicationUserService;

    //    public ActionLinkWithPermissionTagHelper(IApplicationUserService serviceProvider)
    //    {
    //        _applicationUserService =
    //            serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    //    }

    //    public override void Process(TagHelperContext context, TagHelperOutput output)
    //    {
    //        if (OperationSet != null && _applicationUserService.HasAccess(OperationSet))
    //        {
    //            if (context == null)
    //                throw new ArgumentNullException(nameof(context));
    //            if (output == null)
    //                throw new ArgumentNullException(nameof(output));
    //            if (output.Attributes.ContainsName("href"))
    //            {
    //                //if (this.Action != null || this.Controller != null || (this.Area != null || this.Page != null) || (this.PageHandler != null || this.Route != null || (this.Protocol != null || this.Host != null)) || (this.Fragment != null || this._routeValues != null && this._routeValues.Count > 0))
    //                //throw new InvalidOperationException(Resources.FormatAnchorTagHelper_CannotOverrideHref((object)"href", (object)"<a>", (object)"asp-route-", (object)"asp-action", (object)"asp-controller", (object)"asp-area", (object)"asp-route", (object)"asp-protocol", (object)"asp-host", (object)"asp-fragment", (object)"asp-page", (object)"asp-page-handler"));
    //            }
    //            else
    //            {
    //                bool flag1 = this.Route != null;
    //                bool flag2 = this.Controller != null || this.Action != null;
    //                bool flag3 = this.Page != null || this.PageHandler != null;
    //                //if (flag1 & flag2 || flag1 & flag3 || flag2 & flag3)
    //                //throw new InvalidOperationException(string.Join(Environment.NewLine, Resources.FormatCannotDetermineAttributeFor((object)"href", (object)"<a>"), "asp-route", "asp-controller, asp-action", "asp-page, asp-page-handler"));
    //                RouteValueDictionary routeValueDictionary = (RouteValueDictionary)null;
    //                if (this._routeValues != null && this._routeValues.Count > 0)
    //                    routeValueDictionary = new RouteValueDictionary((object)this._routeValues);
    //                if (this.Area != null)
    //                {
    //                    if (routeValueDictionary == null)
    //                        routeValueDictionary = new RouteValueDictionary();
    //                    routeValueDictionary["area"] = (object)this.Area;
    //                }

    //                TagBuilder tagBuilder = !flag3
    //                    ? (!flag1
    //                        ? this.Generator.GenerateActionLink(this.ViewContext, string.Empty, this.Action,
    //                            this.Controller, this.Protocol, this.Host, this.Fragment, (object)routeValueDictionary,
    //                            (object)null)
    //                        : this.Generator.GenerateRouteLink(this.ViewContext, string.Empty, this.Route,
    //                            this.Protocol, this.Host, this.Fragment, (object)routeValueDictionary, (object)null))
    //                    : this.Generator.GeneratePageLink(this.ViewContext, string.Empty, this.Page, this.PageHandler,
    //                        this.Protocol, this.Host, this.Fragment, (object)routeValueDictionary, (object)null);
    //                output.MergeAttributes(tagBuilder);
    //            }

    //        }
    //    }
    //}

    public static class PermissionControls
    {
        private static IServiceProvider _serviceProvider;

        public static void Configure(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }


        private static IHtmlContent GenerateAnchor(IHtmlHelper htmlHelper, string linkText, string action, object routeValues = null)
        {
            var actionContext = _serviceProvider.GetService<IActionContextAccessor>().ActionContext;
            var urlHelper = (IUrlHelper)new UrlHelper(actionContext);
            var controller = urlHelper.ActionContext.RouteData.Values["controller"].ToString();

            var anchor = new TagBuilder("a");
            anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);

            if (routeValues != null)
                anchor.Attributes["href"] = urlHelper.Action(action, routeValues);
            else
                anchor.Attributes["href"] = urlHelper.Action(action);

            return new HtmlString(GetString(anchor));
        }

        private static IHtmlContent GenerateAnchor(IHtmlHelper htmlHelper, string linkText, string action, string controller, object routeValues = null)
        {
            var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();

            var anchor = new TagBuilder("a");
            anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
            
            if (routeValues != null)
                anchor.Attributes["href"] = urlHelper.Action(action, controller, routeValues);
            else
                anchor.Attributes["href"] = urlHelper.Action(action, controller);

            return new HtmlString(GetString(anchor));
        }

        public static string GetString(IHtmlContent content)
        {
            var writer = new StringWriter();
            content.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }

        private static IHtmlContent GenerateAnchorWithController(IHtmlHelper htmlHelper, string linkText, string action, string controller, object routeValues = null)
        {
            var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
            var anchor = new TagBuilder("a");
            anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
            
            if (routeValues != null)
                anchor.Attributes["href"] = urlHelper.Action(action, controller, routeValues);
            else
                anchor.Attributes["href"] = urlHelper.Action(action, controller);

            return new HtmlString(GetString(anchor));
        }


        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, OperationSet operationSet)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
                return GenerateAnchor(htmlHelper, linkText, action);

            return null;
        }

        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, object routeValues, OperationSet operationSet)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
                return GenerateAnchor(htmlHelper, linkText, action, routeValues);

            return new HtmlString(""); //TODO Не уверен что вот так будет работать
        }

        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, string controller, RouteValueDictionary routeValues, OperationSet operationSet)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
                return GenerateAnchor(htmlHelper, linkText, action, controller, routeValues);

            return new HtmlString("");
        }

        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, string controller, object routeValues, OperationSet operationSet)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
                return GenerateAnchorWithController(htmlHelper, linkText, action, controller, routeValues);

            return new HtmlString("");
        }

        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, string controller, OperationSet operationSet)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
                return GenerateAnchorWithController(htmlHelper, linkText, action, controller);

            return new HtmlString("");
        }

        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, object routeValues, object htmlAttributes, OperationSet operationSet)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
            {
                var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
                var anchor = new TagBuilder("a");
                anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
                anchor.Attributes["href"] = urlHelper.Action(action, routeValues);
                anchor.MergeAttributes(new RouteValueDictionary(htmlAttributes));
                return new HtmlString(GetString(anchor));
            }
            return new HtmlString("");
        }

        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes, OperationSet operationSet)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
            {
                var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
                var anchor = new TagBuilder("a");
                anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
                anchor.Attributes["href"] = urlHelper.Action(action, routeValues);
                anchor.MergeAttributes(new RouteValueDictionary(htmlAttributes));
                return new HtmlString(GetString(anchor));
            }
            return new HtmlString("");
        }

        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, string controller, object routeValues, object htmlAttributes, OperationSet operationSet)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
            {
                return GenerateAnchorWithAttributes(htmlHelper, linkText, action, controller, htmlAttributes, routeValues);
            }
            return new HtmlString("");
        }

        private static IHtmlContent GenerateAnchorWithAttributes(IHtmlHelper htmlHelper, string linkText, string action, string controller, object htmlAttributes, object routeValues = null)
        {
            var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
            var anchor = new TagBuilder("a");
            anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
            if (routeValues != null)
                anchor.Attributes["href"] = urlHelper.Action(action, controller, routeValues);
            else
                anchor.Attributes["href"] = urlHelper.Action(action, controller);

            anchor.MergeAttributes(new RouteValueDictionary(htmlAttributes));
            return new HtmlString(GetString(anchor));
        }
        
        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, string controller, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes, OperationSet operationSet)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
            {
                var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
                var anchor = new TagBuilder("a");
                anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
                anchor.Attributes["href"] = urlHelper.Action(action, controller, routeValues);
                anchor.MergeAttributes(new RouteValueDictionary(htmlAttributes));
                return new HtmlString(GetString(anchor));
            }
            return new HtmlString("");
        }
        //TODO Доделать!!!
        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, string controller, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes, OperationSet operationSet)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
            {
                //string value = UrlHelper.GenerateUrl(protocol, hostName, new VirtualPathData(_urlHelper.ActionContext,,new RouteValueDictionary(routeValues)), fragment);
                var anchor = new TagBuilder("a");
                anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
                anchor.MergeAttributes(new RouteValueDictionary(htmlAttributes));
                //anchor.MergeAttribute("href", value);
                return new HtmlString(GetString(anchor));
            }
            return new HtmlString("");
        }
        //TODO доделать!!!
        public static IHtmlContent ActionLinkWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, string controller, string protocol, string hostName, string fragment, RouteValueDictionary routeValues, IDictionary<string, object> htmlAttributes, OperationSet operationSet)
        {
            //NOT TESTED!!!!
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
            {
                //string value = UrlHelper.GenerateUrl(linkText, action, controller, protocol, hostName, fragment, routeValues, null, _urlHelper.ActionContext, false);
                TagBuilder anchor = new TagBuilder("a");
                anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
                anchor.MergeAttributes<string, object>(htmlAttributes);
                //anchor.MergeAttribute("href", value);
                return new HtmlString(GetString(anchor));
            }
            return new HtmlString("");
        }

        public static IHtmlContent IconActionWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, object routeValues, OperationSet operationSet, string glyphiconClass)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
            {
                return GenerateGlyphicon(htmlHelper, linkText, action, routeValues, glyphiconClass);
            }
            return new HtmlString("");
        }


        public static IHtmlContent IconActionWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, string controller, object routeValues, OperationSet operationSet, string glyphiconClass)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
            {
                return GenerateGlyphicon(htmlHelper, linkText, action, controller, routeValues, glyphiconClass);
            }
            return new HtmlString("");
        }
        public static IHtmlContent IconActionWithPermission(this IHtmlHelper htmlHelper, string linkText, string action, object routeValues, OperationSet operationSet, string classAnchor, string glyphiconClass)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(operationSet))
            {
                return GenerateGlyphicon(htmlHelper, linkText, action, routeValues, classAnchor, glyphiconClass);
            }
            return new HtmlString("");
        }

        private static IHtmlContent GenerateGlyphicon(IHtmlHelper htmlHelper, string linkText, string action, object routeValues, string glyphiconClass)
        {
            var actionContext = _serviceProvider.GetService<IActionContextAccessor>().ActionContext;
            var urlHelper = (IUrlHelper)new UrlHelper(actionContext);
            var icon = new TagBuilder("span");
            icon.Attributes["class"] = "glyphicon " + glyphiconClass.Trim();
            icon.Attributes["aria-hidden"] = "true";
            var anchor = new TagBuilder("a");
            anchor.Attributes["href"] = urlHelper.Action(action, routeValues);
            anchor.Attributes["title"] = string.IsNullOrEmpty(linkText) ? "" : linkText;
            anchor.InnerHtml.AppendHtml(icon);
            return new HtmlString(GetString(anchor));
        }

        private static IHtmlContent GenerateGlyphicon(IHtmlHelper htmlHelper, string linkText, string action, string controller, object routeValues, string glyphiconClass)
        {
            var actionContext = _serviceProvider.GetService<IActionContextAccessor>().ActionContext;
            var urlHelper = (IUrlHelper)new UrlHelper(actionContext);
            var icon = new TagBuilder("span");
            icon.Attributes["class"] = "glyphicon " + glyphiconClass.Trim();
            icon.Attributes["aria-hidden"] = "true";
            var anchor = new TagBuilder("a");
            //anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
            anchor.Attributes["href"] = urlHelper.Action(action, controller, routeValues);
            anchor.Attributes["title"] = string.IsNullOrEmpty(linkText) ? "" : linkText;
            anchor.InnerHtml.AppendHtml(icon);
            return new HtmlString(GetString(anchor));
        }

        private static IHtmlContent GenerateGlyphicon(IHtmlHelper htmlHelper, string linkText, string action, object routeValues, string classAnchor, string glyphiconClass)
        {
            var actionContext = _serviceProvider.GetService<IActionContextAccessor>().ActionContext;
            var urlHelper = (IUrlHelper)new UrlHelper(actionContext);
            var icon = new TagBuilder("span");
            icon.Attributes["class"] = "glyphicon " + glyphiconClass.Trim();
            icon.Attributes["aria-hidden"] = "true";
            var anchor = new TagBuilder("a");
            anchor.InnerHtml.AppendHtml(icon);
            anchor.Attributes["href"] = urlHelper.Action(action, routeValues);
            anchor.Attributes["title"] = string.IsNullOrEmpty(linkText) ? "" : linkText;
            anchor.Attributes["class"] = string.IsNullOrEmpty(classAnchor) ? "" : classAnchor;

            return new HtmlString(GetString(anchor));
        }

        public static IHtmlContent IconViewVersionAction(this IHtmlHelper htmlHelper, string linkText, string action, string controller, object routeValues, string glyphiconClass)
        {
            var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
            var icon = new TagBuilder("span");
            icon.Attributes["class"] = "glyphicon " + glyphiconClass.Trim();
            icon.Attributes["aria-hidden"] = "true";
            var anchor = new TagBuilder("a");
            anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
            anchor.Attributes["href"] = urlHelper.Action(action, controller, routeValues);
            anchor.Attributes["title"] = string.IsNullOrEmpty(linkText) ? "" : linkText;

            return new HtmlString(GetString(anchor));
        }
    }


}
