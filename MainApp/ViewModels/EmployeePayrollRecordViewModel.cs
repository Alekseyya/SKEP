using System.Collections.Generic;
using System.Linq;
using Core.Models;


namespace MainApp.ViewModels
{
    public class EmployeePayrollChangeRecordViewModel
    {
        public IEnumerable<EmployeePayrollRecord> FilteredTmpRecords { get; private set; }
        public IEnumerable<EmployeePayrollRecord> TmpRecords { get; private set; }
        public IEnumerable<string> TmpHeaders { get; private set; }

        public EmployeePayrollRecord Record { get; private set; }
        public EmployeePayrollRecord LastTmpRecord { get; private set; }

        public EmployeePayrollChangeRecordViewModel(IEnumerable<EmployeePayrollRecord> tmpRecords = null, EmployeePayrollRecord record = null)
        {
            TmpRecords = tmpRecords == null ? new List<EmployeePayrollRecord>() : tmpRecords;

            if (tmpRecords == null)
                TmpHeaders = new List<string>(0);
            else
                TmpHeaders = tmpRecords.Select(rec => EmployeePayrollRecordTypeHelper.GetDisplayNameFor(rec.RecordType));

            FilteredTmpRecords = TmpRecords.Where(rec => rec.RecordType != EmployeePayrollRecordType.PayrollChangeFin && rec.RecordType != EmployeePayrollRecordType.PayrollChangeHR);

            Record = record == null ? new EmployeePayrollRecord() : record;
            LastTmpRecord = tmpRecords == null ? new EmployeePayrollRecord() :
                tmpRecords.Count() > 0 ? tmpRecords.Last() : new EmployeePayrollRecord();
        }

    }
}
