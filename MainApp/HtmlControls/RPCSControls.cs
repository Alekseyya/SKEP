using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Encodings.Web;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.Controllers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;






namespace MainApp.HtmlControls
{
    /// <summary>
    ///WOW-контролы - генерируют всю необходимую html разметку для поля модели в зависимости от прав пользователя (
    ///если у пользователя есть права на редактирование поля - оно отобразится в виде "Имя поля - <контрол ввода>значение поля</контрол>
    ///если у пользователя есть права на чтение поля - оно отобразится в виде "Имя поля - значение поля
    ///если у пользователя нет прав на это поле - вернется пустая строка. т.е. на View вообще не будет упоминания об этом поле
    /// </summary>

    //[HtmlTargetElement("RPCSDisplayForEmployee")]
    //public class RpcsDisplayForEmployeeTagHelper<TModel, TValue> : TagHelper
    //{
    //    private readonly IApplicationUserService appUserSvc;
    //    public Expression<Func<TModel, TValue>> Expression { get; set; }
    //    public int EmployeeId { get; set; }
    //    public OperationSet OperationSet { get; set; }

    //    public RpcsDisplayForEmployeeTagHelper(IApplicationUserService applicationUserService)
    //    {
    //        appUserSvc =
    //            applicationUserService ?? throw new ArgumentNullException(nameof(applicationUserService));
    //    }

    //    public override void Process(TagHelperContext context, TagHelperOutput output)
    //    {
    //        bool hasAccess = OperationSet != null && appUserSvc.HasAccess(OperationSet);
    //        if (OperationSet != null && (OperationSet.Contains(Operation.EmployeePersonalDataView) &&
    //                                     appUserSvc.HasAccess(Operation.EmployeeSubEmplPersonalDataView)))
    //        {
    //            hasAccess = hasAccess || appUserSvc.IsDepartmentManagerForEmployee(EmployeeId);
    //        }

    //        if (OperationSet != null && (OperationSet.Contains(Operation.EmployeePersonalDataView) &&
    //                                     appUserSvc.GetEmployeeID() == EmployeeId))
    //        {
    //            hasAccess = true;
    //        }

    //        if (hasAccess)
    //        {
    //            Type typeModel = typeof(TModel);
    //            Type typeValue = typeof(TValue);

    //            MemberExpression body = Expression.Body as MemberExpression;
    //            var propertyName = (object) (body.Member as PropertyInfo) != null ? body.Member.Name : (string) null;

    //            //Get value
    //            var someValue = typeValue.GetProperty(propertyName).GetValue(typeValue, null).ToString();

    //            //HtmlString label = HtmlHelperLabelExtensions.LabelFor(html, expression, new { @class = "control-label col-md-4" });
    //            output.TagName = "div";
    //            output.Attributes.SetAttribute("class", "control-label col-md-4");

    //            TagBuilder label = new TagBuilder("label");
    //            label.Attributes.Add("class", "control-label col-md-4");
    //            label.Attributes.Add("for", propertyName);

    //            TagBuilder divContext = new TagBuilder("div");
    //            divContext.Attributes.Add("class", "col-md-8");
    //            divContext.Attributes.Add("style", "padding:6px 12px");
    //            divContext.InnerHtml.Append(someValue);

    //            output.Content.AppendHtml(label);
    //            output.Content.AppendHtml(divContext);

    //            output.TagMode = TagMode.StartTagAndEndTag;

    //        }

    //    }
    //}

    //[HtmlTargetElement("RPCSDisplayProjectTitleView")]
    //public class RpcsDisplayProjectTitleViewTagHelper<TModel, TValue> : TagHelper 
    //{
    //    private readonly IApplicationUserService appUserSvc;
    //    private readonly IHttpContextAccessor _httpContextAccessor;
    //    public Expression<Func<TModel, TValue>> Expression { get; set; }
    //    public Project Project { get; set; }
    //    public string LinkText { get; set; }
    //    public object MyProperty { get; set; }
    //    public OperationSet EditRights{ get; set; }
    //    public OperationSet DisplayRights{ get; set; }

    //    public RpcsDisplayProjectTitleViewTagHelper(IApplicationUserService applicationUserService, IHttpContextAccessor httpContextAccessor)
    //    {
    //        appUserSvc = applicationUserService ?? throw new ArgumentNullException(nameof(applicationUserService));
    //        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    //    }

    //    public override void Process(TagHelperContext context, TagHelperOutput output)
    //    {
    //        //if (appUserSvc.HasAccess(DisplayRights) && appUserSvc.IsMyProject(Project)
    //        //    || appUserSvc.HasAccess(EditRights))
    //        //{
    //        //    var urlHelper = new UrlHelper(_httpContextAccessor.HttpContext.re);
    //        //    IHtmlContent contents = HtmlHelperDisplayExtensions.DisplayFor(html, expression);
    //        //    var anchor = new TagBuilder("a") { InnerHtml = contents.ToString() };
    //        //    anchor.Attributes["href"] = urlHelper.Action(action, "Project", routeValues);
    //        //    anchor.Attributes["title"] = string.IsNullOrEmpty(linkText) ? "" : linkText;

    //        //    return IHtmlContent.Create(GetString(anchor));
    //        //}
    //        //else
    //        //{
    //        //    return HtmlHelperDisplayExtensions.DisplayFor(html, expression);
    //        //}
    //    }
    //}

    public static class RpcsControls
    {
        private static IServiceProvider _serviceProvider;
        

        public static void Configure(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)); ;
        }
        public static IHtmlContent RPCSDisplayForEmployee<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, int employeeId, OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            bool hasAccess = appUserSvc.HasAccess(displayRights);
            if (displayRights.Contains(Operation.EmployeePersonalDataView)
                && appUserSvc.HasAccess(Operation.EmployeeSubEmplPersonalDataView))
            {
                hasAccess = hasAccess || appUserSvc.IsDepartmentManagerForEmployee(employeeId);
            }
            if (displayRights.Contains(Operation.EmployeePersonalDataView)
                && appUserSvc.GetEmployeeID() == employeeId)
            {
                hasAccess = true;
            }
            if (hasAccess)
            {
                var label = html.LabelFor(expression, new { @class = "control-label col-md-4" });
                var contents = html.DisplayFor(expression);
                string result = String.Concat("<div class=\"row\">",
                                                GetString(label),
                                               "<div class=\"col-md-8\" style=\"padding:6px 12px\" >",
                                               GetString(contents),
                                               "</div>",
                                               "</div>");

                return new HtmlString(result);
            }

            return new HtmlString(""); //html.HiddenFor(expression);
        }

        public static string GetString(IHtmlContent content)
        {
            var writer = new StringWriter();
            content.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }

        public static IHtmlContent RPCSDisplayForEmployee<TModel, TValue1, TValue2>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue1>> labelExpression, Expression<Func<TModel, TValue2>> valueExpression, int employeeId, OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            bool hasAccess = appUserSvc.HasAccess(displayRights);
            if (displayRights.Contains(Operation.EmployeePersonalDataView)
                && appUserSvc.HasAccess(Operation.EmployeeSubEmplPersonalDataView))
            {
                hasAccess = hasAccess || appUserSvc.IsDepartmentManagerForEmployee(employeeId);
            }
            if (displayRights.Contains(Operation.EmployeePersonalDataView)
                && appUserSvc.GetEmployeeID() == employeeId)
            {
                hasAccess = true;
            }
            if (hasAccess)
            {
                var label = html.LabelFor(labelExpression, new { @class = "control-label col-md-4" });
                var contents = html.DisplayFor(valueExpression);
                string result = String.Concat("<div class=\"row\">",
                                               GetString(label),
                                               "<div class=\"col-md-8\" style=\"padding:6px 12px\" >",
                                               GetString(contents),
                                               "</div>",
                                               "</div>");

                return new HtmlString(result);
            }

            return new HtmlString(""); // html.HiddenFor(valueExpression);
        }

        public static IHtmlContent RPCSDisplayFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            if (appUserSvc.HasAccess(displayRights))
            {
                var label = html.LabelFor(expression, new { @class = "control-label col-md-4" });
                var contents = html.DisplayFor(expression);
                string result = String.Concat("<div class=\"row\">",
                                               GetString(label),
                                               "<div class=\"col-md-8\" style=\"padding:6px 12px\" >",
                                               GetString(contents),
                                               "</div>",
                                               "</div>");

                return new HtmlString(result);
            }
            return new HtmlString(""); // html.HiddenFor(expression);
        }

        public static IHtmlContent RPCSDisplayFor<TModel, TValue1, TValue2>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue1>> labelExpression, Expression<Func<TModel, TValue2>> valueExpression, OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            if (appUserSvc.HasAccess(displayRights))
            {
                IHtmlContent label = html.LabelFor(labelExpression, new { @class = "control-label col-md-4" });
                IHtmlContent contents = html.DisplayFor(valueExpression);
                string result = String.Concat("<div class=\"row\">",
                                               GetString(label),
                                               "<div class=\"col-md-8\" style=\"padding:6px 12px\" >",
                                               GetString(contents),
                                               "</div>",
                                               "</div>");

                return new HtmlString(result);
            }
            return new HtmlString(""); // html.HiddenFor(expression);
        }

        public static IHtmlContent RPCSDisplayWithItemDetailsViewActionFor<TModel, TValue1, TValue2>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue1>> labelExpression, Expression<Func<TModel, TValue2>> valueExpression, OperationSet displayRights,
            string itemController,
            object itemRouteValues,
            OperationSet viewItemDetailsRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            if (appUserSvc.HasAccess(displayRights))
            {
                IHtmlContent label = HtmlHelperLabelExtensions.LabelFor(html, labelExpression, new { @class = "control-label col-md-4" });
                IHtmlContent contents = null;

                var value = ExpressionMetadataProvider.FromLambdaExpression(valueExpression, html.ViewData, html.MetadataProvider).Model;

                if (appUserSvc.HasAccess(viewItemDetailsRights)
                    && value != null && String.IsNullOrEmpty(value.ToString()) == false)
                {
                    var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
                    string linkText = value.ToString();
                    var anchor = new TagBuilder("a");
                    anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
                    anchor.Attributes["href"] = urlHelper.Action("Details", itemController, itemRouteValues);
                    anchor.Attributes["target"] = "_blank";
                    contents = new HtmlString(GetString(anchor));
                }
                else
                {
                    contents = HtmlHelperDisplayExtensions.DisplayFor(html, valueExpression);
                }
                string result = String.Concat("<div class=\"row\">",
                                               GetString(label),
                                               "<div class=\"col-md-8\" style=\"padding:6px 12px\" >",
                                               GetString(contents),
                                               "</div>",
                                               "</div>");

                return new HtmlString(result);
            }
            return new HtmlString(""); // html.HiddenFor(expression);
        }

        public static IHtmlContent RPCSDisplayWithItemDetailsViewActionForEmployee<TModel, TValue1, TValue2>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue1>> labelExpression, Expression<Func<TModel, TValue2>> valueExpression, int employeeId, OperationSet displayRights,
           string itemController,
           object itemRouteValues,
           OperationSet viewItemDetailsRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            bool hasAccess = appUserSvc.HasAccess(displayRights);
            if (displayRights.Contains(Operation.EmployeePersonalDataView)
                && appUserSvc.HasAccess(Operation.EmployeeSubEmplPersonalDataView))
            {
                hasAccess = hasAccess || appUserSvc.IsDepartmentManagerForEmployee(employeeId);
            }
            if (displayRights.Contains(Operation.EmployeePersonalDataView)
                && appUserSvc.GetEmployeeID() == employeeId)
            {
                hasAccess = true;
            }

            if (hasAccess)
            {
                IHtmlContent label = HtmlHelperLabelExtensions.LabelFor(html, labelExpression, new { @class = "control-label col-md-4" });
                IHtmlContent contents = null;

                var value = ExpressionMetadataProvider.FromLambdaExpression(valueExpression, html.ViewData, html.MetadataProvider).Model;

                if (appUserSvc.HasAccess(viewItemDetailsRights)
                    && value != null && String.IsNullOrEmpty(value.ToString()) == false)
                {
                    var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
                    string linkText = value.ToString();
                    var anchor = new TagBuilder("a");
                    anchor.InnerHtml.Append(string.IsNullOrEmpty(linkText) ? "No Name" : linkText);
                    anchor.Attributes["href"] = urlHelper.Action("Details", itemController, itemRouteValues);
                    anchor.Attributes["target"] = "_blank";
                    contents = new HtmlString(GetString(anchor));
                }
                else
                {
                    contents = HtmlHelperDisplayExtensions.DisplayFor(html, valueExpression);
                }
                string result = String.Concat("<div class=\"row\">",
                                               GetString(label),
                                               "<div class=\"col-md-8\" style=\"padding:6px 12px\" >",
                                               GetString(contents),
                                               "</div>",
                                               "</div>");

                return new HtmlString(result);
            }
            return new HtmlString(""); // html.HiddenFor(expression);
        }

        private static IHtmlContent GenerateAnchor<TModel, TValue>(IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, 
            string linkText, string action, string controller, object routeValues)
        {
            var actionContext = _serviceProvider.GetService<IActionContextAccessor>().ActionContext;
            var urlHelper = (IUrlHelper)new UrlHelper(actionContext);
            IHtmlContent contents = html.DisplayFor(expression);
            var anchor = new TagBuilder("a");
            anchor.InnerHtml.AppendHtml(contents);
            anchor.Attributes["href"] = urlHelper.Action(action, controller, routeValues);
            anchor.Attributes["title"] = string.IsNullOrEmpty(linkText) ? "" : linkText;

            return new HtmlString(GetString(anchor));
        }

        private static IHtmlContent GenerateAnchor<TModel, TValue>(IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression,
            string linkText, string action, object routeValues)
        {
            var actionContext = _serviceProvider.GetService<IActionContextAccessor>().ActionContext;
            var urlHelper = (IUrlHelper)new UrlHelper(actionContext);
            IHtmlContent contents = html.DisplayFor(expression);
            var anchor = new TagBuilder("a");
            anchor.InnerHtml.AppendHtml(contents);
            anchor.Attributes["href"] = urlHelper.Action(action, routeValues);
            anchor.Attributes["title"] = string.IsNullOrEmpty(linkText) ? "" : linkText;

            return new HtmlString(GetString(anchor));
        }

        public static IHtmlContent RPCSDisplayTitleViewActionWithPermissionFor<TModel, TValue>(this IHtmlHelper<TModel> html, 
            Expression<Func<TModel, TValue>> expression, string linkText, string action,string controller, object routeValues, 
            OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            if (appUserSvc.HasAccess(displayRights))
                return GenerateAnchor(html, expression, linkText, action, controller, routeValues);
            else
                return html.DisplayFor(expression);
        }

        public static IHtmlContent RPCSDisplayTitleViewActionWithPermissionFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string linkText, string action, object routeValues, OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            if (appUserSvc.HasAccess(displayRights))
                return GenerateAnchor(html, expression, linkText, action, routeValues);
            else
                return html.DisplayFor(expression);
        }

        public static IHtmlContent RPCSDisplayTitleViewActionWithPermissionFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string linkText, string action, string controller, object routeValues, OperationSet displayRights, bool isBlank = false)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            if (appUserSvc.HasAccess(displayRights))
            {
                var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
                IHtmlContent contents = HtmlHelperDisplayExtensions.DisplayFor(html, expression);
                var anchor = new TagBuilder("a");
                anchor.InnerHtml.Append(contents.ToString());
                anchor.Attributes["href"] = urlHelper.Action(action, controller, routeValues);
                anchor.Attributes["title"] = string.IsNullOrEmpty(linkText) ? "" : linkText;
                if (isBlank)
                {
                    anchor.Attributes["target"] = "_blank";
                    anchor.Attributes["rel"] = "noopener noreferrer";
                }

                return new HtmlString(GetString(anchor));
            }
            else
            {
                return HtmlHelperDisplayExtensions.DisplayFor(html, expression);
            }
        }

        public static IHtmlContent RPCSEditorFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, OperationSet EditorRights, OperationSet displayRights, string toolTip = "", string errorMessage = "")
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            IHtmlContent label = HtmlHelperLabelExtensions.LabelFor(html, expression, new { @class = "control-label col-md-4" });
            if (appUserSvc.HasAccess(EditorRights))
                return GenerateEditor(html, expression, toolTip, errorMessage, label);

            if (appUserSvc.HasAccess(displayRights))
                return GenerateLabel(html, expression, label);

            return HtmlHelperInputExtensions.HiddenFor(html, expression);
        }

        private static IHtmlContent GenerateLabel<TModel, TValue>(IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, IHtmlContent label)
        {
            IHtmlContent contents = HtmlHelperDisplayExtensions.DisplayFor(html, expression);
            string result = String.Concat("<div class=\"form-group\">",
                                           GetString(label),
                                           "<div class=\"col-md-8\"  style=\"padding:6px 12px\" >",
                                           GetString(contents),
                                           "</div>",
                                           GetString(HtmlHelperInputExtensions.HiddenFor(html, expression)),
                                           "</div>");
            return new HtmlString(result);
        }

        private static IHtmlContent GenerateEditor<TModel, TValue>(IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, string toolTip, string errorMessage, IHtmlContent label)
        {
            IHtmlContent contents = HtmlHelperEditorExtensions.EditorFor(html, expression, new { htmlAttributes = new { @class = "form-control", @title = toolTip } });
            IHtmlContent validator = HtmlHelperValidationExtensions.ValidationMessageFor(html, expression, errorMessage, new { @class = "text-danger" });

            string result = String.Concat("<div class=\"form-group\">",
                                           GetString(label),
                                           "<div class=\"col-md-8\">",
                                           GetString(contents),
                                           GetString(validator),
                                           "</div>",
                                           "</div>");

            return new HtmlString(result);
        }

        public static IHtmlContent RPCSDropDownList<TModel, TValue1, TValue2>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue1>> labelExpression, Expression<Func<TModel, TValue2>> valueExpression, string name,
            IEnumerable<SelectListItem> selectList, OperationSet editRights, OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            IHtmlContent label = HtmlHelperLabelExtensions.LabelFor(html, labelExpression, new { @class = "control-label col-md-4" });

            if (appUserSvc.HasAccess(editRights))
            {
                var contents = HtmlHelperSelectExtensions.DropDownList(html, name, selectList, new { @class = "form-control" });
                
                string result = String.Concat("<div class=\"form-group\">",
                                              GetString(label),
                                              "<div class=\"col-md-8\">",
                                              GetString(contents),
                                              "</div>",
                                              "</div>");
                return new HtmlString(result);
            }
            if (appUserSvc.HasAccess(displayRights))
            {
                IHtmlContent contents = HtmlHelperDisplayExtensions.DisplayFor(html, valueExpression);
                string result = String.Concat("<div class=\"form-group\">",
                                                       GetString(label),
                                                       "<div class=\"col-md-8\">",
                                                       GetString(contents),
                                                       "</div>",
                    GetString(HtmlHelperInputExtensions.HiddenFor(html, valueExpression)),
                    GetString(HtmlHelperInputExtensions.HiddenFor(html, labelExpression)),
                                                       "</div>");
                return new HtmlString(result);
            }
            string v = GetString(HtmlHelperInputExtensions.HiddenFor(html, valueExpression));
            string l = GetString(HtmlHelperInputExtensions.HiddenFor(html, labelExpression));
            return new HtmlString(v + l);
        }

        public static IHtmlContent RPCSDisplayProjectTitleView<TModel, TValue>(this IHtmlHelper<TModel> html,
                    Project project,
                    Expression<Func<TModel, TValue>> expression,
                    string linkText, string action, object routeValues, OperationSet editRights, OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            if (appUserSvc.HasAccess(displayRights) && appUserSvc.IsMyProject(project)
                || appUserSvc.HasAccess(editRights))
            {
                var actionContext = _serviceProvider.GetService<IActionContextAccessor>().ActionContext;
                var urlHelper = (IUrlHelper)new UrlHelper(actionContext);
                IHtmlContent contents = HtmlHelperDisplayExtensions.DisplayFor(html, expression);
                var anchor = new TagBuilder("a");
                anchor.InnerHtml.AppendHtml(contents);
                anchor.Attributes["href"] = urlHelper.Action(action, "Project", routeValues);
                anchor.Attributes["title"] = string.IsNullOrEmpty(linkText) ? "" : linkText;

                return new HtmlString(GetString(anchor));
            }
            else
            {
                return html.DisplayFor(expression);
            }
        }

        public static IHtmlContent RPCSMenuIconAction(this IHtmlHelper htmlHelper, string linkText, string actionName, string controllerName, object routeValues, string glyphiconClass)
        {
            var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
            var httpAccessorSvc = _serviceProvider.GetService<IHttpContextAccessor>();
            var scheme = httpAccessorSvc.HttpContext.Request.Scheme;
            var icon = new TagBuilder("span");
            icon.Attributes["class"] = "glyphicon " + glyphiconClass.Trim();
            icon.Attributes["aria-hidden"] = "true";
            var anchor = new TagBuilder("a");
            anchor.InnerHtml.AppendHtml(icon);
            anchor.Attributes["class"] = "navbar-brand";
            anchor.Attributes["href"] = urlHelper.Action(actionName, controllerName, routeValues, scheme);
            anchor.Attributes["title"] = string.IsNullOrEmpty(linkText) ? "" : linkText;
            return new HtmlString(GetString(anchor));
          
        }

        public static IHtmlContent RPCSProjectViewColumnActionLink(this IHtmlHelper htmlHelper, string columnTitle, string action, string controller, string fieldName,
            string currentFilter,
            string currentSortField, string currentSortOrder, ProjectStatus currentStatusFilter, ProjectViewType currentViewType)
        {

            var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
            var anchor = new TagBuilder("a");
            anchor.InnerHtml.Append(string.IsNullOrEmpty(columnTitle) ? "No Name" : columnTitle);
            anchor.Attributes["href"] = urlHelper.Action(action, controller,
                new
                {
                    sortField = fieldName,
                    sortOrder = ((currentSortField != fieldName) ? "" : (currentSortOrder == "desc") ? "" : "desc"),
                    searchString = currentFilter,
                    statusFilter = (int)currentStatusFilter,
                    viewType = (int)currentViewType
                });

            if (String.IsNullOrEmpty(currentSortField) == false
                && currentSortField.Equals(fieldName) == true)
            {
                var sortIcon = new TagBuilder("span");

                if (String.IsNullOrEmpty(currentSortOrder) == false
                    && currentSortOrder == "desc")
                {
                    sortIcon.Attributes["class"] = "glyphicon glyphicon-triangle-bottom";
                }
                else
                {
                    sortIcon.Attributes["class"] = "glyphicon glyphicon-triangle-top";
                }

                return new HtmlString(GetString(anchor) + " " + GetString(sortIcon));
            }
            else
            {
                return new HtmlString(GetString(anchor));
            }

        }
        public static IHtmlContent RPCSProjectViewColumnActionLink(this IHtmlHelper htmlHelper, string columnTitle, string action, string fieldName,
            string currentFilter,
            string currentSortField, string currentSortOrder, ProjectStatus currentStatusFilter, ProjectViewType currentViewType)
        {

            var urlHelper = _serviceProvider.GetRequiredService<IUrlHelper>();
            var anchor = new TagBuilder("a");
            anchor.InnerHtml.Append(string.IsNullOrEmpty(columnTitle) ? "No Name" : columnTitle);
            anchor.Attributes["href"] = urlHelper.Action(action,
                new
                {
                    sortField = fieldName,
                    sortOrder = ((currentSortField != fieldName) ? "" : (currentSortOrder == "desc") ? "" : "desc"),
                    searchString = currentFilter,
                    statusFilter = (int)currentStatusFilter,
                    viewType = (int)currentViewType
                });

            if (String.IsNullOrEmpty(currentSortField) == false
                && currentSortField.Equals(fieldName) == true)
            {
                var sortIcon = new TagBuilder("span");

                if (String.IsNullOrEmpty(currentSortOrder) == false
                    && currentSortOrder == "desc")
                {
                    sortIcon.Attributes["class"] = "glyphicon glyphicon-triangle-bottom";
                }
                else
                {
                    sortIcon.Attributes["class"] = "glyphicon glyphicon-triangle-top";
                }

                return new HtmlString(GetString(anchor) + " " + GetString(sortIcon));
            }
            else
            {
                return new HtmlString(GetString(anchor));
            }

        }
    }
}
