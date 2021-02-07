using System;
using System.Collections.Generic;
using Core.JIRA;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IJiraService
    {
        string GetJson(string url);
        bool IsExternalWorkspaceProjectShortName(string externalWorkspaceProjectShortName);
        Project FindProjectFromExternalWorkspace(Issue issue, string userJiraLogin, DateTime periodStartDate, DateTime periodEndDate);
        List<Worklog> GetWorklogsByIssueId(int issueId);
        string GetProjectShortNameFromEpic(Issue issue);
        string CreateUrlWorklogsByUser(DateTime datesStartDate, DateTime dateEndDate);
        string CreateUrlWorklogsByUser(DateTime datesStartDate, DateTime dateEndDate, string user);
        string GetProjectShortNameFromExternalWorkspace(Issue issue, string userJiraLogin, DateTime periodStartDate, DateTime periodEndDate);
    }
}
