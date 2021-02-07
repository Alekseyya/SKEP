using System;
using System.Text;
using System.Text.RegularExpressions;
using Core.Config;
using Microsoft.Extensions.DependencyInjection;


namespace Core.Helpers
{
    public class RPCSHelper
    {

        private static IServiceProvider _serviceProvider;
        private static CommonConfig _commonConfig;


        public static void Configure(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _commonConfig = _serviceProvider.GetRequiredService<CommonConfig>();
        }

        public static bool IsShowTestCopyWarningMessage()
        {
            bool result = false;

            try
            {
                if ((string)_commonConfig["ShowTestCopyWarningMessage"] != null)
                {
                    result = Boolean.Parse((string)_commonConfig["ShowTestCopyWarningMessage"]);
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static bool IsDissalowInputEmployeePositionAsText()
        {
            bool result = false;

            try
            {
                if ((string)_commonConfig["DissalowInputEmployeePositionAsText"] != null)
                {
                    result = Boolean.Parse((string)_commonConfig["DissalowInputEmployeePositionAsText"]);
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetAlternateExternalEmployeeListURL()
        {
            string result = "";

            try
            {
                if ((string)_commonConfig["AlternateExternalEmployeeListURL"] != null)
                {
                    result = (string)_commonConfig["AlternateExternalEmployeeListURL"];
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string GetAlternateExternalOrgChartListURL()
        {
            string result = "";

            try
            {
                if ((string)_commonConfig["AlternateExternalOrgChartListURL"] != null)
                {
                    result = (string)_commonConfig["AlternateExternalOrgChartListURL"];
                }
            }
            catch (Exception)
            {
            }

            return result;
        }
        public static string NormalizeAndTrimString(string s)
        {
            if (s != null)
            {
                s = s.Replace("\r", "").Replace("\n", " ").Replace("\t", " ").Replace("\u00A0", " ").Replace("\u000B", " ").Trim();
            }

            return s;
        }

        public static string FindAndReplaceAllUrls(string text)
        {
            var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var mathes = linkParser.Matches(text);
            if (mathes.Count > 0)
            {
                var builder = new StringBuilder(text);

                foreach (Match m in linkParser.Matches(text))
                {
                    builder.Replace(m.Value, $"<a target='_blank' href='{m.Value}'>{m.Value}</a>");
                }
                return builder.ToString();
            }
            else
            {
                return text;
            }
        }

        public static string TextFormat(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            // var linkParser = new Regex(@"\r\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return Regex.Replace(text.Trim(), @"\r\n|\n", "<br>");
        }
    }
}
