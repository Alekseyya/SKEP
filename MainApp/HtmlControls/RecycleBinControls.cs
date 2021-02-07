using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Extensions;
using Core.Models.Attributes;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;



namespace MainApp.HtmlControls
{
    public static class RecycleBinControls
    {

        public static IHtmlContent RecycleBinHeaderCell(this IHtmlHelper html, object model)
        {
            var entity = model.GetType().GetProperty("Entity").GetValue(model, null);
            if (entity != null)
            {
                var propertiesWithCustomAttributes = entity.GetType().GetProperties()
                    .Where(prop => Attribute.IsDefined(prop, typeof(DisplayInRecycleBinAttribute)))
                    .ToList();
                var htmlText = string.Empty;
                if (propertiesWithCustomAttributes.Count > 0)
                {
                    var sortedPropertyinfoByOrder = propertiesWithCustomAttributes.OrderBy(p =>
                        p.GetCustomAttributes(typeof(DisplayInRecycleBinAttribute), true)[0]
                            .GetType().GetProperty("Order").GetValue(p.GetCustomAttributes(typeof(DisplayInRecycleBinAttribute), true)[0], null));

                    var tagId = new TagBuilder("th");
                    tagId.InnerHtml.AppendHtml(html.Label(entity.GetType().GetProperty("ID").GetDisplayName()).ToString());
                    htmlText += tagId;

                    foreach (var propertyInfo in sortedPropertyinfoByOrder)
                    {
                        var tag = new TagBuilder("th");
                        var propertyName = propertyInfo.GetDisplayName();
                        tag.InnerHtml.AppendHtml(html.Label(propertyName).ToString());
                        htmlText += tag;
                    }

                    var baseObject = entity.GetType().BaseType;
                    var tagDeletedDate = new TagBuilder("th");
                    tagDeletedDate.InnerHtml.AppendHtml(html.Label(baseObject.GetProperty("DeletedDate").GetDisplayName()).ToString());
                    htmlText += tagDeletedDate;
                    var tagDeletedBy = new TagBuilder("th");
                    tagDeletedBy.InnerHtml.AppendHtml(html.Label(baseObject.GetProperty("DeletedBy").GetDisplayName()).ToString());
                    htmlText += tagDeletedBy;

                    return new HtmlString(htmlText);
                }

                var vProperty = entity.GetType().GetProperty("FullName");
                if (vProperty == null)
                    vProperty = entity.GetType().GetProperty("Title");
                if (vProperty == null)
                    vProperty = entity.GetType().GetProperty("ShortName");
                if (vProperty != null && !string.IsNullOrEmpty(vProperty.GetDisplayName()))
                {
                    var tagId = new TagBuilder("th");
                    tagId.InnerHtml.AppendHtml(html.Label(entity.GetType().GetProperty("ID").GetDisplayName()).ToString());
                    htmlText += tagId;

                    var tag = new TagBuilder("th");
                    tag.InnerHtml.AppendHtml(vProperty.GetDisplayName());
                    htmlText += tag;

                    var baseObject = entity.GetType().BaseType;
                    var tagDeletedDate = new TagBuilder("th");
                    tagDeletedDate.InnerHtml.AppendHtml(html.Label(baseObject.GetProperty("DeletedDate").GetDisplayName()).ToString());
                    htmlText += tagDeletedDate;
                    var tagDeletedBy = new TagBuilder("th");
                    tagDeletedBy.InnerHtml.AppendHtml(html.Label(baseObject.GetProperty("DeletedBy").GetDisplayName()).ToString());
                    htmlText += tagDeletedBy;
                    return new HtmlString(htmlText);
                }

                return HtmlString.Empty;
            }
            return HtmlString.Empty;
        }

        public static IHtmlContent RecycleBinDataCell(this IHtmlHelper html, object model)
        {
            var entity = model.GetType().GetProperty("Entity").GetValue(model, null);
            if (entity != null)
            {
                var propertiesWithCustomAttributes = entity.GetType().GetProperties()
                    .Where(prop => Attribute.IsDefined(prop, typeof(DisplayInRecycleBinAttribute)))
                    .ToList();
                var htmlText = string.Empty;
                if (propertiesWithCustomAttributes.Count > 0)
                {
                    var sortedPropertyinfoByOrder = propertiesWithCustomAttributes.OrderBy(p =>
                        p.GetCustomAttributes(typeof(DisplayInRecycleBinAttribute), true)[0]
                            .GetType().GetProperty("Order").GetValue(p.GetCustomAttributes(typeof(DisplayInRecycleBinAttribute), true)[0], null));

                    var tagId = new TagBuilder("td");
                    tagId.InnerHtml.AppendHtml(entity.GetType().GetProperty("ID").GetValue(entity, null).ToString());
                    htmlText += tagId;

                    foreach (var propertyInfo in sortedPropertyinfoByOrder)
                    {
                        var tag = new TagBuilder("td");
                        var propertyValue = propertyInfo.GetValue(entity, null).ToString();
                        if (propertyInfo.GetValue(entity, null).GetType() == typeof(DateTime))
                            tag.InnerHtml.AppendHtml(Convert.ToDateTime(propertyInfo.GetValue(entity, null)).ToShortDateString());
                        else if ((propertyInfo.PropertyType == typeof(int?) || propertyInfo.PropertyType == typeof(int)) && propertyInfo.Name.EndsWith("ID"))
                            tag.InnerHtml.AppendHtml(propertyInfo.GetBaseNameInLink(entity));
                        else if (propertyInfo.PropertyType.IsEnum)
                            tag.InnerHtml.AppendHtml(Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsEnum).FirstOrDefault(x => x.Name == propertyInfo.PropertyType.Name)
                            .GetFields().FirstOrDefault(f => f.Name == propertyValue).GetCustomAttribute<DisplayAttribute>().Name);
                        else
                            tag.InnerHtml.AppendHtml(propertyValue);

                        htmlText += tag;
                    }

                    var baseObject = entity.GetType().BaseType;
                    var tagDeletedDate = new TagBuilder("td");
                    tagDeletedDate.InnerHtml.AppendHtml(baseObject.GetProperty("DeletedDate").GetValue(entity, null).ToString());
                    htmlText += tagDeletedDate;
                    var tagDeletedBy = new TagBuilder("td");
                    tagDeletedBy.InnerHtml.AppendHtml(baseObject.GetProperty("DeletedBy").GetValue(entity, null).ToString());
                    htmlText += tagDeletedBy;

                    return new HtmlString(htmlText);
                }

                return DefaultClassWithoutDisplayInRecycleBinAttribute(entity, "td");
            }
            return HtmlString.Empty;
        }

        private static IHtmlContent DefaultClassWithoutDisplayInRecycleBinAttribute(object entity, string tag)
        {
            var htmlText = string.Empty;
            var vProperty = entity.GetType().GetProperty("FullName");
            if (vProperty == null)
                vProperty = entity.GetType().GetProperty("Title");
            if (vProperty == null)
                vProperty = entity.GetType().GetProperty("ShortName");
            if (vProperty != null && vProperty.GetValue(entity, null) != null)
            {
                var tagId = new TagBuilder(tag);
                tagId.Attributes.Add("id", "id");
                tagId.InnerHtml.AppendHtml(entity.GetType().GetProperty("ID").GetValue(entity, null).ToString());
                htmlText += tagId;

                var tagBuilderTd = new TagBuilder(tag);
                tagBuilderTd.InnerHtml.AppendHtml(vProperty.GetValue(entity, null).ToString());
                htmlText += tagBuilderTd;

                htmlText += RecycleBinInformationDeletedCell(entity, tag);
                return new HtmlString(htmlText);
            }
            return HtmlString.Empty;
        }

        private static string RecycleBinInformationDeletedCell(object entity, string tag)
        {
            var htmlText = string.Empty;
            var baseObject = entity.GetType().BaseType;
            var tagDeletedDate = new TagBuilder(tag);
            tagDeletedDate.InnerHtml.AppendHtml(baseObject.GetProperty("DeletedDate").GetValue(entity, null).ToString());
            htmlText += tagDeletedDate;
            var tagDeletedBy = new TagBuilder(tag);
            tagDeletedBy.InnerHtml.AppendHtml(baseObject.GetProperty("DeletedBy").GetValue(entity, null).ToString());
            htmlText += tagDeletedBy;
            return htmlText;
        }


    }
}
