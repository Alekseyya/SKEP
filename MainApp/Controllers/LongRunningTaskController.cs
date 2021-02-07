using System;
using System.Linq;
using Core.Common;
using Core.Helpers;
using Core.Models.RBAC;
using MainApp.Helpers;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;



namespace MainApp.Controllers
{
    public class LongRunningTaskController : Controller
    {
        public const string ErrorReportIDPostfix = "_ErrorReport";

        private readonly IMemoryCache _memoryCache;

        public LongRunningTaskController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        public ActionResult Index()
        {
            // ReSharper disable once Mvc.ViewNotResolved
            return View();
        }


       
        [HttpGet]
        public virtual ActionResult DownloadReportHtml(string fileId)
        {
            if (_memoryCache.Get(fileId) != null)
            {
                string htmlBody = _memoryCache.Get(fileId) as string;
                string html = RPCSHtmlHelper.GetSimpleUTF8HtmlPage("Отчет", htmlBody);

                byte[] binData = System.Text.Encoding.UTF8.GetBytes(html);
                _memoryCache.Remove(fileId);
                return File(binData, "text/HTML", "Report" + DateTime.Now.ToString("ddMMyyHHmmss") + ".html");
            }
            else
            {
                string html = RPCSHtmlHelper.GetSimpleUTF8HtmlPage("Ошибка", "Произошла ошибка при выполнении операции.");
                byte[] binData = System.Text.Encoding.UTF8.GetBytes(html);
                _memoryCache.Remove(fileId);
                return File(binData, "text/HTML", "Error" + DateTime.Now.ToString("ddMMyyHHmmss") + ".html");
            }
        }


        [HttpGet]
        public virtual ActionResult DownloadExcel(string fileId)
        {
            if (_memoryCache.Get(fileId) != null)
            {
                byte[] binData = (_memoryCache.Get(fileId) as byte[]).ToArray();
                _memoryCache.Remove(fileId);
                return File(binData, ExcelHelper.ExcelContentType,
                    "Report" + DateTime.Now.ToString("ddMMyyHHmmss") + ".xlsx");
            }
            else
            {
                string html = RPCSHtmlHelper.GetSimpleUTF8HtmlPage("Ошибка", "Произошла ошибка при выполнении операции.");

                if (_memoryCache.Get(fileId + ErrorReportIDPostfix) != null)
                {
                    html = RPCSHtmlHelper.GetSimpleUTF8HtmlPage("Ошибка", "Произошла ошибка при выполнении операции." + _memoryCache.Get(fileId + ErrorReportIDPostfix));
                    _memoryCache.Remove(fileId + ErrorReportIDPostfix);
                }

                byte[] binData = System.Text.Encoding.UTF8.GetBytes(html);
                _memoryCache.Remove(fileId);
                return File(binData, "text/HTML", "Error" + DateTime.Now.ToString("ddMMyyHHmmss") + ".html");
            }
        }


        #region ServiceController
        [OperationActionFilter(nameof(Operation.ADSyncAccess))]
        public ContentResult GetImportDataFromADCurrentProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        [OperationActionFilter(nameof(Operation.ADSyncAccess))]
        public ContentResult GetSyncADProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        [OperationActionFilter(nameof(Operation.BitrixSyncAccess))]
        public ContentResult GetSyncWithBitrixProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        [OperationActionFilter(nameof(Operation.TimesheetProcessingAccess))]
        public ContentResult GetTimesheetProcessingProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        public ContentResult GetImportTSHoursFromExcelProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        public ContentResult GetImportBudgetLimitRecordsFromExcelProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public ContentResult GetDBDataProcessing(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        #endregion


        #region ReportsController

        [AProjectsHoursReportView]
        public ContentResult GetGenerateProjectsHoursReportCurrentProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        [OperationActionFilter(nameof(Operation.ProjectsCostsReportView))]
        public ContentResult GetGenerateProjectsCostsReportCurrentProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        /*[AProjectDetailsView]*/
        public ContentResult GetGenerateProjectsHoursForPMReportCurrentProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        [OperationActionFilter(nameof(Operation.FinReportView))]
        public ContentResult GetGenerateDKReportCurrentProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        /*[AProjectDetailsView]*/
        public ContentResult GetGeneratePMDKReportCurrentProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        [OperationActionFilter(nameof(Operation.FinReportView))]
        public ContentResult GetGenerateAFPReportCurrentProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        [OperationActionFilter(nameof(Operation.FinReportView))]
        public ContentResult GetGenerateQualifyingRoleRateReportCurrentProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }

        [OperationActionFilter(nameof(Operation.TSHoursUtilizationReportView))]
        public ContentResult GetGenerateTSHoursUtilizationReportCurrentProgress(string id)
        {
            ControllerContext.HttpContext.Response.Headers.Add("cache-control", "no-cache");

            return Content("{ \"status\" : \"" + LongRunningTaskBase.GetStatus(id).ToString() + "\", \"statusMessage\" : \"" + LongRunningTaskBase.GetStatusMessage(id).ToString() + "\" }");
        }


        #endregion

    }
}
