using HtmlTags;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Szlem.AspNetCore.Views
{
    public static class HtmlHelperExtensions
    {
        public static HtmlTag DlHeaderRow<T, TResult>(this IHtmlHelper<T> htmlHelper, Expression<Func<T, TResult>> expression)
        {
            return new HtmlTag("dt").AddClass("col-sm-3").AppendHtml(htmlHelper.DisplayNameFor(expression));
        }

        public static HtmlTag DlDataRowOrEmptyValue<T, TResult>(this IHtmlHelper<T> htmlHelper, Expression<Func<T, TResult>> expression, string emptyValue)
        {
            var model = htmlHelper.ViewContext.ViewData.Model;
            var content = expression.Compile().Invoke((T)model);
            if (IsNullOrEmpty(content))
            {
                return new HtmlTag("dd").AddClass("col-sm-9").Append(new HtmlTag("span").AddClass("text-muted").AppendHtml(emptyValue));
            }
            else
            {
                using (var stringWriter = new StringWriter())
                {
                    var display = htmlHelper.DisplayFor(expression);
                    display.WriteTo(stringWriter, System.Text.Encodings.Web.HtmlEncoder.Default);
                    return new HtmlTag("dd").AddClass("col-sm-9").AppendHtml(stringWriter.ToString());
                }
            }
        }

        public static IHtmlContent DlFullRowOrEmptyValue<T, TResult>(this IHtmlHelper<T> helper, Expression<Func<T, TResult>> expression, string emptyValue)
        {
            return new HtmlContentBuilder()
                .AppendHtml(helper.DlHeaderRow(expression))
                .AppendHtml(helper.DlDataRowOrEmptyValue(expression, emptyValue));
        }

        public static IHtmlContent DlFullRow<T, TResult>(this IHtmlHelper<T> helper, Expression<Func<T, TResult>> expression)
        {
            return new HtmlContentBuilder()
                .AppendHtml(helper.DlHeaderRow(expression))
                .AppendHtml(helper.DlDataRowOrEmptyValue(expression, string.Empty));
        }

        private static bool IsNullOrEmpty<T>(T value)
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
                return true;
            if (value is string s && string.IsNullOrWhiteSpace(s))
                return true;
            return false;
        }
    }
}
