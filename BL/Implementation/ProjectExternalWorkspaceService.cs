using System;
using System.Collections.Generic;
using System.Text;
using Core.BL;

using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;




namespace BL.Implementation
{
    public class ProjectExternalWorkspaceService : RepositoryAwareServiceBase<ProjectExternalWorkspace, int, IProjectExternalWorkspaceRepository>, IProjectExternalWorkspaceService
    {
        public ProjectExternalWorkspaceService(IRepositoryFactory repositoryFactory) : base(repositoryFactory)
        {
        }
    }
}
