using System;

namespace Core.Helpers
{
    public class ProjectHelper
    {
        public static string GetProjectSortKeyByProjectShortName(string projectShortName)
        {
            string projectSortKey = "";

            if (projectShortName.IndexOf(".16") > 0)
            {
                projectSortKey = "16";
            }
            else if (projectShortName.IndexOf(".17") > 0)
            {
                projectSortKey = "17";
            }
            else if (projectShortName.IndexOf(".18") > 0)
            {
                projectSortKey = "18";
            }
            else if (projectShortName.IndexOf(".19") > 0)
            {
                projectSortKey = "19";
            }
            else
            {
                projectSortKey = "99";
            }

            if (projectShortName.IndexOf("AFL") == 0)
            {
                projectSortKey += "1AFL-";
            }
            else if (projectShortName.IndexOf("AVDR") == 0)
            {
                projectSortKey += "2AVDR-";
            }
            else if (projectShortName.IndexOf("AKR") == 0)
            {
                projectSortKey += "3AKR-";
            }
            else if (projectShortName.IndexOf("LSY") == 0)
            {
                projectSortKey += "4LSY-";
            }
            else if (projectShortName.IndexOf("PR") == 0)
            {
                projectSortKey += "5PR-";
            }
            else if (projectShortName.IndexOf("VTB") == 0)
            {
                projectSortKey += "6VTB-";
            }
            else if (projectShortName.IndexOf("ATEX") == 0)
            {
                projectSortKey += "7ATEX-";
            }
            else if (projectShortName.IndexOf("OTTA") == 0)
            {
                projectSortKey += "8OTTA-";
            }
            else if (projectShortName.IndexOf("SKK") == 0)
            {
                projectSortKey += "9SKK-";
            }
            else if (projectShortName.IndexOf("RGPR") == 0)
            {
                projectSortKey += "ARGPR-";
            }
            projectSortKey += "Z" + projectShortName;

            return projectSortKey;
        }

        public static bool IsNoPaidProject(string projectShortName)
        {
            bool result = false;

            if (String.IsNullOrEmpty(projectShortName) == false)
            {
                if (projectShortName.Contains("RGRP.CORP.VACNOPAD.") == true)
                {
                    result = true;
                }
            }

            return result;
        }

        public static int GetProjectYear(string projectShortName)
        {
            int projectYear = 2000;

            if (String.IsNullOrEmpty(projectShortName) == false)
            {
                try
                {
                    int index = projectShortName.LastIndexOf(".");

                    if (index != -1)
                    {
                        projectYear = 2000 + Convert.ToInt32(projectShortName.Substring(index + 1).Trim());
                    }
                }
                catch (Exception)
                {

                }
            }

            return projectYear;
        }
    }
}
