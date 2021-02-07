using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;


namespace Core.Extensions
{
    public static class StringExtention
    {
        public static string ToSnakeCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) { return input; }

            var startUnderscores = Regex.Match(input, @"^_+");
            return startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
        }

        public static Decimal ConvertToDecimalWihtCommaHundredths(this string str)
        {
            var result = str.Split(',')[0] + "," + string.Join("", str.Split(',')[1].Take(2));
            return Convert.ToDecimal(result);
        }

        public static Decimal ConvertToDecimalWihtPointHundredths(this string str)
        {
            return Convert.ToDecimal(str.Split('.')[0] + "," + string.Join("", str.Split('.')[1].Take(2)));
        }

        public static Decimal ConvertToDecimal(this string str)
        {
            if (str == null)
                return 0;
            if (str.IndexOf(',') != -1)
            {
               return ConvertToDecimalWihtCommaHundredths(str);
            }
            else if (str.IndexOf('.') != -1)
               return ConvertToDecimalWihtPointHundredths(str);
            return Convert.ToDecimal(str);
        }

        public static string TruncateAtWord(this string value, int length)
        {
            if (value == null || value.Length < length || value.IndexOf(" ", length) == -1)
                return value;

            return value.Substring(0, value.IndexOf(" ", length));
        }
        public static string TruncateAtWordWitchDots(this string input, int length)
        {
            if (input == null || input.Length < length)
                return input;
            int iNextSpace = input.LastIndexOf(" ", length, StringComparison.Ordinal);
            return string.Format("{0}…", input.Substring(0, (iNextSpace > 0) ? iNextSpace : length).Trim());
        }

        public static string RemoveUnwantedHtmlTags(this string encodedHtml)
        {
            var decodeDescription = HttpUtility.HtmlDecode(encodedHtml);
            var doc = new HtmlDocument();
            doc.LoadHtml(decodeDescription);
            return doc.DocumentNode.InnerText;
        }

    }
}