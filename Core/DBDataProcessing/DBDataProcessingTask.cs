using System;
using System.Collections.Generic;
using System.Linq;
using Core.BL.Interfaces;
using Core.Common;


namespace Core.DBDataProcessing
{
    public class DBDataProcessingTask : LongRunningTaskBase
    {
        private readonly IEmployeeOrganisationService _employeeOrganisationService;
        // private readonly IOrganisationService _organisationService;
        // private readonly IEmployeePositionOfficialService _employeePositionOfficialService;
        private readonly IEmployeeService _employeeService;

        public DBDataProcessingTask(IEmployeeOrganisationService employeeOrganisationService,
            // IOrganisationService organisationService,
            // IEmployeePositionOfficialService employeePositionOfficialService,
            IEmployeeService employeeService)
        {
            _employeeOrganisationService = employeeOrganisationService;
            // _organisationService = organisationService;
            // _employeePositionOfficialService = employeePositionOfficialService;
            _employeeService = employeeService;
        }

        private void ProcessEmployee()
        {
            var currentDate = DateTime.Today;
            var employees = _employeeService.GetAllEmployees();
            double emplCount = employees.Count;
            var emplOrganisationList = _employeeOrganisationService.Get(o => o.ToList());

            SetStatus(1, "Обновление данных с БД - загрузка", true);
            int i = 0;

            foreach (var employee in employees)
            {
                i++;
                var emplOrgRecordList = emplOrganisationList.Where(eo => eo.EmployeeID == employee.ID
                && (!eo.OrganisationDateEnd.HasValue || eo.OrganisationDateEnd.Value.Date >= currentDate.Date)
                && (currentDate.Date >= eo.OrganisationDateBegin.Value.Date));

                var mainRecord = emplOrgRecordList.FirstOrDefault(eo => eo.IsMainPlaceWork);
                if (mainRecord != null || emplOrgRecordList.Count() == 1)
                {
                    mainRecord = emplOrgRecordList.First();
                    employee.OrganisationID = mainRecord.OrganisationID;
                    employee.EmployeePositionOfficialID = mainRecord.EmployeePositionOfficialID;
                }
                else
                {
                    employee.OrganisationID = null;
                    employee.EmployeePositionOfficialID = null;
                }
                _employeeService.UpdateWithoutVersion(employee);

                int statusValue = (int)((i / emplCount) * 99);
                SetStatus(1 + statusValue, "Обработка данных сотрудника: " + employee.FullName);
            }
        }
        public DBDataProcessingTaskResult ProcessLongRunningAction(string userIdentityName, string id)
        {
            var htmlReport = string.Empty;
            var htmlErrorReport = string.Empty;
            taskId = id;
            taskReport = new LongRunningTaskReport("Отчет о обработки данных с БД", "");

            SetStatus(1, "Обновление данных с БД - старт", true);

            try
            {
                ProcessEmployee();
                SetStatus(100, "Синхронизация завершена", true);
                try
                {
                    if (taskReport != null)
                        htmlReport = taskReport.GenerateHtmlReport();
                }
                catch (Exception ex)
                {
                    SetStatus(-1, "Ошибка: " + ex.Message);
                    htmlErrorReport += "<br>" + ex.Message + "<br>" + ex.StackTrace + "<br>" + ex.TargetSite.ToString();
                }
            }
            catch (Exception ex)
            {
                SetStatus(-1, "Ошибка: " + ex.Message);
                htmlErrorReport += "<br>" + ex.Message + "<br>" + ex.StackTrace + "<br>" + ex.TargetSite.ToString();
            }
            return new DBDataProcessingTaskResult() { fileId = id, fileHtmlReport = new List<string>() { htmlReport, htmlErrorReport } };
        }
    }
}
