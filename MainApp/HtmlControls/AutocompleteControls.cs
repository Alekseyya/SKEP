using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Encodings.Web;
using System.Web;
using Core.BL.Interfaces;
using Core.Extensions;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;





namespace MainApp.HtmlControls
{

    //[HtmlTargetElement("RPCSAutocompleteSearchControl")]
    //public class RpcsAutocompleteSearchControlTagHelper<TMolde> : TagHelper
    //{
    //    private readonly IHttpContextAccessor _httpContextAccessor;
    //    private readonly IHtmlGenerator _htmlGenerator;
    //    public string InputName { get; set; }
    //    public string ViewBagName { get; set; }
    //    public string HtmlAttributes { get; set; }

    //    [HtmlAttributeNotBound]
    //    [ViewContext]
    //    public ViewContext ViewContext { get; set; }

    //    public RpcsAutocompleteSearchControlTagHelper(IHttpContextAccessor httpContextAccessor, IHtmlGenerator htmlGenerator)
    //    {
    //        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    //        _htmlGenerator = htmlGenerator ?? throw new ArgumentNullException(nameof(htmlGenerator));
    //    }

    //    public override void Process(TagHelperContext context, TagHelperOutput output)
    //    {
    //        var data = ViewContext.ViewData[ViewBagName];
    //        if (data != null && data is SelectList)
    //        {
    //            string dropDownName = InputName + "Combo";
    //            string searchValue = "";
    //            if (!String.IsNullOrEmpty(_httpContextAccessor.HttpContext.Request.Query[InputName]))
    //            {
    //                searchValue = _httpContextAccessor.HttpContext.Request.Query[InputName];
    //            }

    //            var selectContext = CreateTagHelperContext();
    //            var selectOutput = CreateTagHelperOutput("select");


    //            SelectList controlData = data as SelectList;
    //            SelectTagHelper selectTagHelper = new SelectTagHelper(_htmlGenerator)
    //            {
    //                Items = controlData,
    //                ViewContext = ViewContext
    //            };
    //            selectTagHelper.Process(selectContext, selectOutput);

    //            string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').searchcombobox();  $('#{0}').blur(); SetPostbackValue('{1}', '{2}'); }});</script>",
    //                dropDownName, InputName, HttpUtility.UrlPathEncode(searchValue));

    //            output.TagName = "div";
    //            output.Attributes.Add("id", "searchBox");
    //            output.Content.AppendHtml(selectOutput);
    //            output.Content.SetHtmlContent(initScript);
    //        }

    //    }

    //    private static TagHelperContext CreateTagHelperContext()
    //    {
    //        return new TagHelperContext(
    //            new TagHelperAttributeList(),
    //            new Dictionary<object, object>(),
    //            Guid.NewGuid().ToString("N"));
    //    }

    //    private static TagHelperOutput CreateTagHelperOutput(string tagName)
    //    {
    //        return new TagHelperOutput(
    //            tagName,
    //            new TagHelperAttributeList(),
    //            (a, b) =>
    //            {
    //                var tagHelperContent = new DefaultTagHelperContent();
    //                tagHelperContent.SetContent(string.Empty);
    //                return Task.FromResult<TagHelperContent>(tagHelperContent);
    //            });
    //    }
    //}

    //[HtmlTargetElement("RPCSAutocompleteEnumDropDownList")]

    //public class RpcsAutocompleteEnumDropDownListTaghelper<TModel, TEnum, TProperty> : TagHelper
    //{
    //    private readonly IHttpContextAccessor _httpContextAccessor;
    //    private readonly IApplicationUserService appUserSvc;
    //    private readonly IHtmlGenerator _htmlGenerator;

    //    public Expression<Func<TModel, TEnum>> Expression { get; set; }
    //    public Expression<Func<TModel, TProperty>> DisplayExpression { get; set; }
    //    public Operation EditRights { get; set; }
    //    public Operation DisplayRights { get; set; }

    //    [HtmlAttributeNotBound]
    //    [ViewContext]
    //    public ViewContext ViewContext { get; set; }

    //    public RpcsAutocompleteEnumDropDownListTaghelper(IHttpContextAccessor httpContextAccessor, IApplicationUserService applicationUserService , IHtmlGenerator htmlGenerator)
    //    {
    //        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    //        appUserSvc = applicationUserService ?? throw new ArgumentNullException(nameof(applicationUserService));
    //        _htmlGenerator = htmlGenerator ?? throw new ArgumentNullException(nameof(htmlGenerator));
    //    }

    //    public override void Process(TagHelperContext context, TagHelperOutput output)
    //    {
    //        if (Expression == null)
    //        {
    //            throw new ArgumentNullException("expression");
    //        }

    //        if (!typeof(TEnum).IsEnum)
    //        {
    //            throw new ArgumentException("TEnum");
    //        }



    //        MemberExpression body = Expression.Body as MemberExpression;
    //        var propertyName = (object)(body.Member as PropertyInfo) != null ? body.Member.Name : (string)null;

    //        ////ApplicationUser user = UsersFactory.Instance.GetUser(HttpContext.Current.User);
    //        //MvcHtmlString label = HtmlHelperLabelExtensions.LabelFor(html, expression, new { @class = "control-label col-md-4" });

    //        var labelContext = CreateTagHelperContext();
    //        var labelOutput = CreateTagHelperOutput("label");
    //        labelOutput.Attributes.Add("for", propertyName);
    //        labelOutput.Attributes.Add("class", "control-label col-md-4");
    //        var label = new LabelTagHelper(_htmlGenerator)
    //        {
    //            ViewContext = ViewContext
    //        };
    //        label.Process(labelContext, labelOutput);


    //        if (appUserSvc.HasAccess(EditRights))
    //        {
    //            string controlName = AutocompleteControls.GetExpressionParam<TModel, TEnum>(Expression);
    //            string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').combobox();  }});</script>", controlName);

    //            //ModelMetadata metadata = ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData);
    //            IList<SelectListItem> selectList = Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
    //                .Select(x => new SelectListItem
    //                {
    //                    Text = ((Enum)(object)x).GetAttributeOfType<DisplayAttribute>().Name,
    //                    Value = Convert.ToUInt64(x).ToString()
    //                }).ToList();

    //            var selectContext = CreateTagHelperContext();
    //            var selectOutput = CreateTagHelperOutput("select");
    //            //MvcHtmlString contents = HtmlHelperSelectExtensions.DropDownListFor(html, expression, selectList, new { @class = "form-control" });

    //            //SelectTagHelper selectTagHelper = new SelectTagHelper(_htmlGenerator)
    //            //{
    //            //    For = 
    //            //    Items = 
    //            //    ViewContext = ViewContext
    //            //};




    //            //string result = String.Concat("<div class=\"form-group\">",
    //            //    GetString(label),
    //            //    "<div class=\"col-md-8\">",
    //            //    GetString(contents),
    //            //    "</div>",
    //            //    "</div>");
    //            //return new HtmlString(result);
    //        }

    //        //if (appUserSvc.HasAccess(displayRights))
    //        //{
    //        //   var contents = HtmlHelperDisplayExtensions.DisplayFor(html, displayExpression);
    //        //    string result = String.Concat("<div class=\"form-group\">",
    //        //        GetString(label),
    //        //        "<div class=\"col-md-8\" style=\"padding:8px 12px\" >",
    //        //        GetString(contents),
    //        //        "</div>",
    //        //        "</div>");
    //        //    return new HtmlString(result);
    //        //}
    //        //string v = GetString(HtmlHelperInputExtensions.HiddenFor(html, displayExpression));
    //        //string l = GetString(HtmlHelperInputExtensions.HiddenFor(html, expression))
    //        //return new HtmlString(v + l);
    //    }

    //    private static TagHelperContext CreateTagHelperContext()
    //    {
    //        return new TagHelperContext(
    //            new TagHelperAttributeList(),
    //            new Dictionary<object, object>(),
    //            Guid.NewGuid().ToString("N"));
    //    }

    //    private static TagHelperOutput CreateTagHelperOutput(string tagName)
    //    {
    //        return new TagHelperOutput(
    //            tagName,
    //            new TagHelperAttributeList(),
    //            (a, b) =>
    //            {
    //                var tagHelperContent = new DefaultTagHelperContent();
    //                tagHelperContent.SetContent(string.Empty);
    //                return Task.FromResult<TagHelperContent>(tagHelperContent);
    //            });
    //    }

    //}

    //[HtmlTargetElement("RPCSAutocompleteDropDownList")]
    //public class RPCSAutocompleteDropDownList<TModel, TProperty1, TProperty2>(
    //                                                                            this HtmlHelper<TModel> html,
    //                                                                            Expression<Func<TModel, TProperty1>> expression,
    //                                                                            Expression<Func<TModel, TProperty2>> displayExpression,
    //                                                                            IEnumerable<SelectListItem> selectList,
    //                                                                            string optionLabel,
    //                                                                            Operation editRights,
    //                                                                            Operation displayRights)
    //{
    //    private readonly IApplicationUserService appUserSvc;

    //    public RPCSAutocompleteDropDownList(IApplicationUserService applicationUserService)
    //    {
    //        appUserSvc = applicationUserService ?? throw new ArgumentNullException(nameof(applicationUserService));
    //    }

    //    //ApplicationUser user = UsersFactory.Instance.GetUser(HttpContext.Current.User);
    //    var aa = HtmlHelperHtmlHelperLabelExtensions.LabelFor()
    //   var label = HtmlHelperLabelExtensions.LabelFor(html, expression, new { @class = "control-label col-md-4" });

    //    if (appUserSvc.HasAccess(editRights))
    //    {
    //        string controlName = GetExpressionParam<TModel, TProperty1>(expression);
    //        string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').combobox();  }});</script>", controlName);
    //       var contents = HtmlHelperSelectExtensions.DropDownListFor(html, expression, selectList, optionLabel, new { @class = "form-control" });
    //        string result = String.Concat(
    //                                      GetString(contents),
    //                                      initScript
    //                                      );
    //        return new HtmlString(result);
    //    }

    //    if (appUserSvc.HasAccess(displayRights))
    //    {
    //       var contents = HtmlHelperDisplayExtensions.DisplayFor(html, displayExpression);
    //        return contents;
    //    }

    //    string v = GetString(HtmlHelperInputExtensions.HiddenFor(html, displayExpression));
    //    string l = GetString(HtmlHelperInputExtensions.HiddenFor(html, expression))
    //    return new HtmlString(v + l);
    //}


    public static class AutocompleteControls
    {
        //private static IApplicationUserService appUserSvc;
        private static IServiceProvider _serviceProvider;

        public static void Configure(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public static IHtmlContent RPCSAutocompleteSearchControl<TModel>(this IHtmlHelper<TModel> html,
                                                                          string inputName,
                                                                          string viewBagName,
                                                                          object htmlAttributes)
        {
            var data = html.ViewData[viewBagName];
            if (data != null && data is SelectList)
            {
                string dropDownName = inputName + "Combo";
                string searchValue = "";
                var httpContextSvc = _serviceProvider.GetRequiredService<IHttpContextAccessor>();

                if (!String.IsNullOrEmpty(httpContextSvc.HttpContext.Request.Query[inputName]))
                {
                    searchValue = httpContextSvc.HttpContext.Request.Query[inputName];
                }

                SelectList controlData = data as SelectList;
                var contents = html.DropDownList(dropDownName, controlData, string.Empty, htmlAttributes);
                string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').searchcombobox();  $('#{0}').blur(); SetPostbackValue('{1}', '{2}'); }});</script>",
                    dropDownName, inputName, HttpUtility.UrlPathEncode(searchValue).Replace(@"\", @"\\"));

                string result = String.Concat("<div id='searchBox'>",
                                         GetString(contents),
                                         initScript,
                                         "</div>"
                                         );
                return new HtmlString(result);
            }
            return new HtmlString("");
        }

        public static IHtmlContent RPCSAutocompleteEnumDropDownList<TModel, TEnum>(this IHtmlHelper<TModel> html,
            Expression<Func<TModel, TEnum>> expression,
            SelectList selectList)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum");
            }

            var label = html.LabelFor(expression, new { @class = "control-label col-md-4" });
            var contents = html.DropDownListFor(expression, selectList, new { @class = "form-control" });
            string result = String.Concat("<div class=\"form-group\">",
                GetString(label),
                "<div class=\"col-md-8\">",
                GetString(contents),
                "</div>",
                "</div>");
            return new HtmlString(result);
        }

        public static IHtmlContent RPCSAutocompleteEnumDropDownList<TModel, TEnum, TProperty>(
                                                                                    this IHtmlHelper<TModel> html,
                                                                                    Expression<Func<TModel, TEnum>> expression,
                                                                                    Expression<Func<TModel, TProperty>> displayExpression,
                                                                                    OperationSet editRights,
                                                                                    OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum");
            }

            var label = html.LabelFor(expression, new { @class = "control-label col-md-4" });

            if (appUserSvc.HasAccess(editRights))
            {
                string controlName = GetExpressionParam<TModel, TEnum>(expression);
                string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').combobox();  }});</script>", controlName);

                var metadata = ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData, html.MetadataProvider);
                IList<SelectListItem> selectList = Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                    .Select(x => new SelectListItem
                    {
                        Text = ((Enum)(object)x).GetAttributeOfType<DisplayAttribute>().Name,
                        Value = Convert.ToUInt64(x).ToString()
                    }).ToList();

                var contents = html.DropDownListFor(expression, selectList, new { @class = "form-control" });
                string result = String.Concat("<div class=\"form-group\">",
                    GetString(label),
                    "<div class=\"col-md-8\">",
                    GetString(contents),
                    "</div>",
                    "</div>");
                return new HtmlString(result);
            }

            string l = GetString(html.HiddenFor(expression));

            if (appUserSvc.HasAccess(displayRights))
            {
                var contents = html.DisplayFor(displayExpression);
                string result = String.Concat("<div class=\"form-group\">",
                    GetString(label),
                    "<div class=\"col-md-8\" style=\"padding:8px 12px\" >",
                    GetString(contents),
                    "</div>",
                    "</div>");
                return new HtmlString(result + l);
            }
            return new HtmlString(l);
        }
        public static IHtmlContent RPCSAutocompleteEnumDropDownList<TModel, TEnum>(
            this IHtmlHelper<TModel> html,
            Expression<Func<TModel, TEnum>> expression,
            SelectList selectList,
            OperationSet editRights,
            OperationSet displayRights)
        {

            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum");
            }

            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            var label = html.LabelFor(expression, new { @class = "control-label col-md-4" });

            if (appUserSvc.HasAccess(editRights) || appUserSvc.HasAccess(displayRights))
            {

                var contents = html.DropDownListFor(expression, selectList, new { @class = "form-control" });
                string result = String.Concat("<div class=\"form-group\">",
                    GetString(label),
                    "<div class=\"col-md-8\">",
                    GetString(contents),
                    "</div>",
                    "</div>");
                return new HtmlString(result);
            }
            return new HtmlString("");
        }


        public static IHtmlContent RPCSAutocompleteEnumDropDownList<TModel, TEnum, TProperty>(
                                                                                    this IHtmlHelper<TModel> html,
                                                                                    Expression<Func<TModel, TEnum>> expression,
                                                                                    Expression<Func<TModel, TProperty>> displayExpression,
                                                                                    string lableClassAttribute,
                                                                                    bool validationAttribute,
                                                                                    OperationSet editRights,
                                                                                    OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum");
            }

            //ApplicationUser user = UsersFactory.Instance.GetUser(HttpContext.Current.User);
            var label = html.LabelFor(expression, new { @class = "control-label " + lableClassAttribute });

            if (appUserSvc.HasAccess(editRights))
            {
                string controlName = GetExpressionParam<TModel, TEnum>(expression);
                string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').combobox();  }});</script>", controlName);

                var metadata = ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData, html.MetadataProvider);
                IList<SelectListItem> selectList = Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                    .Select(x => new SelectListItem
                    {
                        Text = ((Enum)(object)x).GetAttributeOfType<DisplayAttribute>().Name,
                        Value = Convert.ToUInt64(x).ToString()
                    }).ToList();

                var contents = html.DropDownListFor(expression, selectList, new { @class = "form-control" });

                string result = string.Empty;
                if (validationAttribute)
                    result = String.Concat("<div class=\"form-group\">",
                                                GetString(label),
                                                "<div class=\"col-md-8\">",
                                                 GetString(contents),
                                                    html.ValidationMessageFor(expression, "", new { @class = "text-danger" }),
                                                "</div>",
                                            "</div>");
                else
                    result = String.Concat("<div class=\"form-group\">",
                                                GetString(label),
                                                "<div class=\"col-md-8\">",
                                                    GetString(contents),
                                                "</div>",
                                           "</div>");
                return new HtmlString(result);
            }
            string l = GetString(html.HiddenFor(expression));

            if (appUserSvc.HasAccess(displayRights))
            {
                var contents = html.DisplayFor(displayExpression);
                string result = String.Concat("<div class=\"form-group\">",
                    GetString(label),
                    "<div class=\"col-md-8\" style=\"padding:8px 12px\" >",
                    GetString(contents),
                    "</div>",
                    "</div>");
                return new HtmlString(result + l);
            }
            return new HtmlString(l);
        }



        public static IHtmlContent RPCSAutocompleteDropDownList<TModel, TProperty1, TProperty2>(
                                                                        this IHtmlHelper<TModel> html,
                                                                        Expression<Func<TModel, TProperty1>> expression,
                                                                        Expression<Func<TModel, TProperty2>> displayExpression,
                                                                        IEnumerable<SelectListItem> selectList,
                                                                        string optionLabel,
                                                                        OperationSet editRights,
                                                                        OperationSet displayRights)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            var label = HtmlHelperLabelExtensions.LabelFor(html, expression, new { @class = "control-label col-md-4" });

            if (appUserSvc.HasAccess(editRights))
            {
                string controlName = GetExpressionParam<TModel, TProperty1>(expression);
                string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').combobox();  }});</script>", controlName);
                
                var contents = html.DropDownListFor(expression, selectList, optionLabel, new { @class = "form-control" });
                string result = String.Concat(
                                              GetString(contents),
                                              initScript
                                              );
                return new HtmlString(result);
            }
            string l = GetString(html.HiddenFor(expression));

            if (appUserSvc.HasAccess(displayRights))
            {
                var contents = html.DisplayFor(displayExpression);
                string result = GetString(contents);
                return new HtmlString(result + l);
            }
            return new HtmlString(l);
        }
        public static string GetString(IHtmlContent content)
        {
            var writer = new StringWriter();
            content.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }


        public static IHtmlContent RPCSAutocompleteDropDownList<TModel, TValue1, TValue2>(
            this HtmlHelper<TModel> html,
            Expression<Func<TModel, TValue1>> labelExpression,
            Expression<Func<TModel, TValue2>> valueExpression,
            OperationSet editRights,
            OperationSet displayRights)
        {
            return RPCSAutocompleteDropDownList<TModel, TValue1, TValue2>(html, labelExpression, valueExpression, editRights, displayRights, null);
        }

        public static IHtmlContent RPCSAutocompleteDropDownList<TModel, TValue1, TValue2>(
                                                                        this IHtmlHelper<TModel> html,
                                                                        Expression<Func<TModel, TValue1>> labelExpression,
                                                                        Expression<Func<TModel, TValue2>> valueExpression,
                                                                        OperationSet editRights,
                                                                        OperationSet displayRights, object htmlAttributes)
        {
            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();

            var label = html.LabelFor(labelExpression, new { @class = "control-label col-md-4" });

            if (appUserSvc.HasAccess(editRights))
            {
                var dropDown = GenerateAutocompleteDropDown(html, labelExpression, htmlAttributes);
                string result = String.Concat("<div class=\"form-group\">",
                                              GetString(label),
                                              "<div class=\"col-md-8\">",
                                              GetString(dropDown),
                                              "</div>",
                                              "</div>");
                return new HtmlString(result);
            }
            string l = GetString(html.HiddenFor(labelExpression));

            if (appUserSvc.HasAccess(displayRights))
            {
                var contents = html.DisplayFor(valueExpression);
                string result = String.Concat("<div class=\"form-group\">",
                                                       GetString(label),
                                                       "<div class=\"col-md-8\" style=\"padding:8px 12px\" >",
                                                       GetString(contents),
                                                       "</div>",
                                                       "</div>");
                return new HtmlString(result + l);
            }
            return new HtmlString(l);
        }


        public static IHtmlContent RPCSAutocompleteDropDownList<TModel, TValue1, TValue2>(
                                                                        this IHtmlHelper<TModel> html,
                                                                        Expression<Func<TModel, TValue1>> labelExpression,
                                                                        Expression<Func<TModel, TValue2>> valueExpression,
                                                                        OperationSet editRights,
                                                                        OperationSet displayRights)
        {

            var appUserSvc = _serviceProvider.GetRequiredService<IApplicationUserService>();
            var label = html.LabelFor(labelExpression, new { @class = "control-label col-md-4" });

            if (appUserSvc.HasAccess(editRights))
            {
                var dropDown = GenerateDropDown(html, labelExpression);
                string result = String.Concat("<div class=\"form-group\">",
                                              GetString(label),
                                              "<div class=\"col-md-8\">",
                                              GetString(dropDown),
                                              "</div>",
                                              "</div>");
                return new HtmlString(result);
            }
            string l = GetString(html.HiddenFor(labelExpression));

            if (appUserSvc.HasAccess(displayRights))
            {
                var contents = HtmlHelperDisplayExtensions.DisplayFor(html, valueExpression);
                string result = String.Concat("<div class=\"form-group\">",
                                                       GetString(label),
                                                       "<div class=\"col-md-8\" style=\"padding:8px 12px\" >",
                                                       GetString(contents),
                                                       "</div>",
                                                       "</div>");
                return new HtmlString(result + l);
            }
            return new HtmlString(l);
        }

        public static IHtmlContent RPCSAutocompleteDropDownList<TModel, TValue1>(
                                                                        this IHtmlHelper<TModel> html,
                                                                        Expression<Func<TModel, TValue1>> labelExpression)
        {
            var label = HtmlHelperLabelExtensions.LabelFor(html, labelExpression, new { @class = "control-label col-md-4" });

            var dropDown = GenerateDropDown(html, labelExpression);
            string result = String.Concat("<div class=\"form-group\">",
                                          GetString(label),
                                          "<div class=\"col-md-8\">",
                                          GetString(dropDown),
                                          "</div>",
                                          "</div>");
            return new HtmlString(result);
        }

        public static IHtmlContent RPCSAutocompleteDropDownListClear<TModel, TValue1>(
                                                                        this IHtmlHelper<TModel> html,
                                                                        Expression<Func<TModel, TValue1>> labelExpression)
        {
            var dropDown = GenerateDropDown(html, labelExpression);
            return dropDown;
        }

        public static IHtmlContent RPCSAutocompleteDropDownListBySelectList<TModel, TProperty>(this IHtmlHelper<TModel> html,
            Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            return RPCSAutocompleteDropDownListBySelectList<TModel, TProperty>(html, expression, selectList, null, htmlAttributes);
        }

        public static IHtmlContent RPCSAutocompleteDropDownListBySelectList<TModel, TProperty>(this IHtmlHelper<TModel> html,
            Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel,
            object htmlAttributes)
        {
            string controlName = GetExpressionParam<TModel, TProperty>(expression);
            string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').combobox();  }});</script>", controlName);

            var contents = html.DropDownListFor(expression, selectList, optionLabel, htmlAttributes);
            string result = String.Concat(
                                          GetString(contents),
                                          initScript
                                          );
            return new HtmlString(result);
        }

        public static IHtmlContent RPCSAutocompleteDropDownListBySelectList<TModel>(
                                                                        this IHtmlHelper<TModel> html,
                                                                        string name, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes)
        {
            string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').combobox();  }});</script>", name);
            var contents = html.DropDownList(name, selectList, optionLabel, htmlAttributes);
            string result = String.Concat(
                                          GetString(contents),
                                          initScript
                                          );
            return new HtmlString(result);
        }

        private static IHtmlContent GenerateDropDown<TModel, TValue1>(IHtmlHelper<TModel> html,
            Expression<Func<TModel, TValue1>> expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel)
        {
            string controlName = GetExpressionParam<TModel, TValue1>(expression);
            string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').combobox();  $('#{0}').blur(); }});</script>", controlName);
            //var contents = HtmlHelperSelectExtensions.DropDownListFor(html, expression, selectList, optionLabel, new { @class = "form-control" });
            //TODO переделать!!
            var contents = html.DropDownListFor(expression, selectList, optionLabel, new { @class = "form-control" });
            string result = String.Concat(
                                          GetString(contents),
                                          initScript
                                          );
            return new HtmlString(result);
        }


        private static IHtmlContent GenerateDropDown<TModel, TValue1>(IHtmlHelper<TModel> html,
                                                                       Expression<Func<TModel, TValue1>> labelExpression)
        {
            string controlName = GetExpressionParam<TModel, TValue1>(labelExpression);
            string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').combobox();  $('#{0}').blur(); }});</script>", controlName);
            var contents = html.DropDownListFor(labelExpression, null, new { @class = "form-control" });
            string result = String.Concat(
                                          GetString(contents),
                                          initScript
                                          );
            return new HtmlString(result);
        }

        private static IHtmlContent GenerateAutocompleteDropDown<TModel, TValue1>(IHtmlHelper<TModel> html,
            Expression<Func<TModel, TValue1>> labelExpression, object htmlAttributes)
        {
            string controlName = GetExpressionParam<TModel, TValue1>(labelExpression);
            string initScript = string.Format("<script>$(document).ready(function () {{ $('#{0}').combobox();  $('#{0}').blur(); }});</script>", controlName);
            var contents = (htmlAttributes == null) ? html.DropDownListFor(labelExpression, null, " - не выбрано - ", new { @class = "form-control" }) : html.DropDownListFor(labelExpression, null, " - не выбрано - ", htmlAttributes);
            string result = String.Concat(
                GetString(contents),
                initScript
            );
            return new HtmlString(result);
        }

        public static string GetExpressionParam<TModel, TValue>(Expression<Func<TModel, TValue>> expession)
        {
            string exprName = expession.Parameters.FirstOrDefault().Name;
            string exprBody = expession.Body.ToString();

            string clearBody = exprBody.Replace(exprName + ".", "");
            return clearBody;
        }

    }
}