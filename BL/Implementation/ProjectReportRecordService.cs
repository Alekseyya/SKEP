using System;
using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
    public class ProjectReportRecordService : RepositoryAwareServiceBase<ProjectReportRecord, int, IProjectReportRecordsRepository>, IProjectReportRecordService
    {
        public ProjectReportRecordService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
