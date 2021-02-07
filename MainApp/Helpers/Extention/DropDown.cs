using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace MainApp.Helpers.Extention
{
    //public static class DropDown
    //{
    //    public static IHtmlContent DropDownListFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes)
    //    {
    //        return DropDownListFor<TModel, TProperty>(htmlHelper, expression, selectList, optionLabel, (IDictionary<string, object>)HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
    //    }

    //    public static IHtmlContent DropDownListFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, string optionLabel, IDictionary<string, object> htmlAttributes)
    //    {
    //        if (expression == null)
    //            throw new ArgumentNullException(nameof(expression));
    //        ModelMetadata metadata = ExpressionMetadataProvider.FromLambdaExpression<TModel, TProperty>(expression, htmlHelper.ViewData, htmlHelper.MetadataProvider).Metadata;
    //        return DropDownListHelper((HtmlHelper)htmlHelper, metadata, ExpressionHelper.GetExpressionText((LambdaExpression)expression), selectList, optionLabel, htmlAttributes);
    //    }
    //    private static IHtmlContent DropDownListHelper(HtmlHelper htmlHelper, ModelMetadata metadata, string expression, IEnumerable<SelectListItem> selectList, string optionLabel, IDictionary<string, object> htmlAttributes)
    //    {
    //        return htmlHelper.SelectInternal(metadata, optionLabel, expression, selectList, false, htmlAttributes);
    //    }

    //    private static IHtmlContent SelectInternal(this HtmlHelper htmlHelper, ModelMetadata metadata, string optionLabel, string name, IEnumerable<SelectListItem> selectList, bool allowMultiple, IDictionary<string, object> htmlAttributes)
    //    {
    //        //string fullHtmlFieldName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
    //        //if (string.IsNullOrEmpty(fullHtmlFieldName))
    //        //    throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(name));
    //        //bool flag = false;
    //        //if (selectList == null)
    //        //{
    //        //    selectList = htmlHelper.GetSelectData(name);
    //        //    flag = true;
    //        //}
    //        //object defaultValue = allowMultiple ? htmlHelper.GetModelStateValue(fullHtmlFieldName, typeof(string[])) : htmlHelper.GetModelStateValue(fullHtmlFieldName, typeof(string));
    //        //if (defaultValue == null && !string.IsNullOrEmpty(name))
    //        //{
    //        //    if (!flag)
    //        //        defaultValue = htmlHelper.ViewData.Eval(name);
    //        //    else if (metadata != null)
    //        //        defaultValue = metadata.Model;
    //        //}
    //        //if (defaultValue != null)
    //        //    selectList = SelectExtensions.GetSelectListWithDefaultValue(selectList, defaultValue, allowMultiple);
    //        //StringBuilder stringBuilder = SelectExtensions.BuildItems(optionLabel, selectList);
    //        //TagBuilder tagBuilder = new TagBuilder("select")
    //        //{
    //        //    InnerHtml = stringBuilder.ToString()
    //        //};
    //        //tagBuilder.MergeAttributes<string, object>(htmlAttributes);
    //        //tagBuilder.MergeAttribute(nameof(name), fullHtmlFieldName, true);
    //        //tagBuilder.GenerateId(fullHtmlFieldName);
    //        //if (allowMultiple)
    //        //    tagBuilder.MergeAttribute("multiple", "multiple");
    //        //ModelState modelState;
    //        //if (htmlHelper.ViewData.ModelState.TryGetValue(fullHtmlFieldName, out modelState) && modelState.Errors.Count > 0)
    //        //    tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
    //        //tagBuilder.MergeAttributes<string, object>(htmlHelper.GetUnobtrusiveValidationAttributes(name, metadata));
    //        //return tagBuilder.ToMvcHtmlString(TagRenderMode.Normal);
    //        return null;
    //   }
    //}
}
