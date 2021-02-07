using System.Collections.Generic;
using System.Data;

namespace Core.BL.Interfaces
{
    public interface IExcelService
    {
        byte[] CreateBinaryByDataTable(DataTable dataTable, string sheetName, string title);
        DataTable CreateDataTableByEntryList<T>(List<T> list);
        DataTable CreateDataTableColumnsByEntryWithType<T>(T entryType);
    }
}
