using System;
using System.Collections.Generic;
using System.Text;

namespace Core.BL.Interfaces
{
    public interface IServiceService
    {
        (bool hasRelated, string relatedInDBClassId) HasRecycleBinInDBRelation<T>(T entry);
        bool RecycleBinRestoreInTable(string tableName, int restoreId);
        bool RecycleBinDeleteInTable(string tableName, int deletedId);
    }
}
