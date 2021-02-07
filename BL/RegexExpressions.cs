using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RMX.RPCS.BL
{
    public static class RegexExpressions
    {
        //AFL.AB.DEVAFRD.18.GAR
        //AFL.AB.DEVAFRD18.18.GAR
        //AFL.AB.18.18.GAR
        private static readonly string _projectsWithStageCode = "((?:[a-z][a-z]+))(\\.)((?:[a-z][a-z]+))(\\.)((.)+)(\\.)(\\d+)(\\.)((?:[a-z][a-z0-9_]*))";

        //Проект принадлежит к мультипроекту
        //AFL.SB.PAGEEPR-M.18
        //AFL.SBIX.PAGEEPR-M.18
        //AFLA.SBIXG.PAGEEPRRR-M.7
        private static readonly string _projectsBelongsToMultiproject =
            "((?:[a-z][a-z\\.\\d\\-]+)\\.(?:[a-z][a-z\\-]+))(?![\\w\\.])(-)((?:[a-z][a-z0-9_]*))(\\.)(\\d+)";

        //Проекты без статус кода
        //RGRP.CORP.BDGT.19
        //RGRP.CORP19.BDGT.19 
        //RGRP.CORP19.19.19 
        private static readonly string _projectsWithoutStageCode =
            "((?:[a-z][a-z]+))(\\.)((.)+)(\\.)((.)+)(\\.)(\\d+)";

        public static bool IsMatch(string searchString)
        {
            if (Regex.IsMatch(searchString, _projectsWithStageCode, RegexOptions.IgnoreCase | RegexOptions.Singleline)
                || Regex.IsMatch(searchString, _projectsWithoutStageCode,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                || Regex.IsMatch(searchString, _projectsBelongsToMultiproject,
                    RegexOptions.IgnoreCase | RegexOptions.Singleline))
                return true;
            return false;
        }
        
    }
}