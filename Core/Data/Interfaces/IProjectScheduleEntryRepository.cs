using System;
using System.Collections.Generic;
using System.Text;
using Core.Models;


namespace Core.Data.Interfaces
{
    public interface IProjectScheduleEntryRepository : IRepository<ProjectScheduleEntry, int>
    {
        ProjectScheduleEntry GetVersion(int projectScheduleEntryId, int version);
        IList<ProjectScheduleEntry> GetVersions(int projectScheduleEntryId);
        IList<ProjectScheduleEntry> GetVersions(int projectScheduleEntryId, bool withChangeInfo);
    }
}
