using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Core.Common;
using Oracle.ManagedDataAccess.Client;

namespace Core.BL.Interfaces
{
    public interface ITimesheetService
    {
        bool IsExternalTSAllowed();
        void GetDataFromTSAutoHoursRecords(LongRunningTaskBase task);
        void GetDataFromTSHoursRecords(LongRunningTaskBase task, DateTime periodStart, DateTime periodEnd, bool useTSHoursRecordsOnly);
        //void GetDataFromTSHoursRecordsForProject(LongRunningTaskBase task, DateTime startPeriod, DateTime endPeriod, string projectShortName);
        bool GetDataFromTimeSheetDB(LongRunningTaskBase task, string dateBegin, string dateEnd, int monthCount, bool getVacations);
        void GetProjectsFromTimeSheetDB(LongRunningTaskBase task);
        void GetDataFromTimeSheetDBForPM(LongRunningTaskBase task, string dateBegin, string dateEnd,
            ArrayList projectShortNames, int monthCount);

        byte[] GetProjectsConsolidatedHoursReportExcel(LongRunningTaskBase task, string userIdentityName,
            string reportPeriodName, string reportTitle,
            bool saveResultsInDB,
            DataTable employeePayrollSheetDataTable, DataTable projectsOtherCostsSheetDataTable,
            DateTime periodStart, DateTime periodEnd,
            List<int> departmentsIDs);
        byte[] GetProjectsHoursReportExcel(LongRunningTaskBase task, string userIdentityName, string reportPeriodName,
            string reportTitle, int monthWorkHours,
            bool saveResultsInDB,
            bool addToReportNotInDBEmplyees,
            bool showEmployeeDataInReport,
            bool groupByMonth,
            DataTable employeePayrollSheetDataTable, DataTable projectsOtherCostsSheetDataTable,
            DateTime periodStart, DateTime periodEnd,
            List<int> departmentsIDs);
        byte[] GetProjectsHoursForPMReportExcel(LongRunningTaskBase task, string userIdentityName, string reportTitle,
            string projectShortName,
            DateTime periodStart, DateTime periodEnd);

        void SyncTSHoursRecordsWithExternalTimesheet(LongRunningTaskBase task,
            LongRunningTaskReport report,
            DateTime periodStart, DateTime periodEnd,
            bool deleteSyncedRecordsBeforeSync, bool updateAlreadyAddedRecords,
            bool stopOnError,
            int batchSaveRecordsLimit);
        void SyncVacationRecordsWithExternalTimesheet(LongRunningTaskBase task,
            LongRunningTaskReport report,
            DateTime periodStart, DateTime periodEnd,
            bool deleteSyncedRecordsBeforeSync, bool updateAlreadyAddedRecords,
            bool stopOnError,
            int batchSaveRecordsLimit);
        void createPROJECTS(LongRunningTaskBase task, OracleConnection conn);
        void createPROJECTSForPM(LongRunningTaskBase task, OracleConnection conn,
            ArrayList projectShortNames);
        void createPERMANENT_PROJECTS(LongRunningTaskBase task, OracleConnection conn);
        void createHOURS(LongRunningTaskBase task, OracleConnection conn, String dateBegin, String dateEnd,
            int monthCount);
        void createHOURSForPM(LongRunningTaskBase task, OracleConnection conn, String dateBegin, String dateEnd,
            int monthCount);
        void createVACATIONS(LongRunningTaskBase task, OracleConnection conn, String dateBegin, String dateEnd);
    }
}
