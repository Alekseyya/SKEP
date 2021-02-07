using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Core.BL.Interfaces;
using Core.Config;
using Core.Extensions;
using Core.Helpers;
using Core.JIRA;
using Core.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BL.Implementation
{
    public class JiraService : IJiraService
    {
        private readonly IUserService _userService;
        private readonly IProjectExternalWorkspaceService _projectExternalWorkspaceService;
        private readonly IProjectService _projectService;
        private readonly JiraConfig _jiraConfig;

        public JiraService(IUserService userService, IOptions<JiraConfig> jiraOptions, IProjectExternalWorkspaceService projectExternalWorkspaceService, IProjectService projectService)
        {
            _userService = userService;
            _projectExternalWorkspaceService = projectExternalWorkspaceService;
            _projectService = projectService;
            _jiraConfig = jiraOptions.Value;
        }
        public string GetJson(string url)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Credentials = new NetworkCredential(_jiraConfig.ApiUrl, _jiraConfig.ApiPassword);
            var encode = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(_jiraConfig.ApiUser + ":" + _jiraConfig.ApiPassword));
            request.Headers.Add("Authorization", "Basic " + encode);
            request.PreAuthenticate = true;
            HttpWebResponse httpWebResponse = (HttpWebResponse)request.GetResponse();

            var responseJson = String.Empty;
            using (var reader = new StreamReader(httpWebResponse.GetResponseStream()))
            {
                responseJson = reader.ReadToEnd();
            }
            return responseJson;
        }
        private string CheckingAmbiguityOfProjectCodes(Issue issue)
        {
            var jiraSupportPRCCustomFieldProject = _projectService.GetByShortName(issue.Fields.JiraSupportPRCCustomField);
            var jiraPRCCustomFieldProject = _projectService.GetByShortName(issue.Fields.JiraPRCCustomField);
            var jiraEpicNameCustomFieldProject = _projectService.GetByShortName(issue.Fields.JiraEpicNameCustomField);

            //ТП != RPCS, ТП!= EpicName, RPCS != EpicName //неоднозначность кодов проекта
            if ((jiraSupportPRCCustomFieldProject != null && jiraPRCCustomFieldProject != null && jiraSupportPRCCustomFieldProject.ShortName != jiraPRCCustomFieldProject.ShortName) ||
                (jiraSupportPRCCustomFieldProject != null && jiraEpicNameCustomFieldProject != null && jiraSupportPRCCustomFieldProject.ShortName != jiraEpicNameCustomFieldProject.ShortName) ||
                (jiraPRCCustomFieldProject != null && jiraEpicNameCustomFieldProject != null && jiraPRCCustomFieldProject.ShortName != jiraEpicNameCustomFieldProject.ShortName))
                return nameof(ErrorTypesJira.ProjectCodeNotDefined);
            if (jiraSupportPRCCustomFieldProject != null)
                return jiraSupportPRCCustomFieldProject.ShortName;
            if (jiraPRCCustomFieldProject != null)
                return jiraPRCCustomFieldProject.ShortName;
            if (jiraEpicNameCustomFieldProject != null)
                return jiraEpicNameCustomFieldProject.ShortName;

            return string.Empty;
        }

        public string GetProjectShortNameFromEpic(Issue issue)
        {
            var projectShortName = CheckingAmbiguityOfProjectCodes(issue);
            var searchUrl = string.Empty;
            var responseJson = string.Empty;
            //если есть ссылка на эпик и не найден код в задаче(тогда идем в епик)
            if (string.IsNullOrEmpty(projectShortName) && !string.IsNullOrEmpty(issue.Fields.JiraEpicLinkCustomField))
            {
                searchUrl = _jiraConfig.ApiUrl + issue.Fields.JiraEpicLinkCustomField;
                responseJson = GetJson(searchUrl);
                var responseIssueObjectEpicTask = JsonConvert.DeserializeObject<Issue>(responseJson);
                if (responseIssueObjectEpicTask != null)
                    projectShortName = CheckingAmbiguityOfProjectCodes(responseIssueObjectEpicTask);
            }
            //если есть у поздадачи задачи и у подзадачи не найден код проекта
            else if (string.IsNullOrEmpty(projectShortName) && issue.Fields.Parent != null)
            {
                searchUrl = _jiraConfig.ApiUrl + issue.Fields.Parent.ParentProject;
                responseJson = GetJson(searchUrl);
                var jiraApiEpicLinkCustomField = JsonConvert.DeserializeObject<Issue>(responseJson).Fields.JiraEpicLinkCustomField;
                if (!string.IsNullOrEmpty(jiraApiEpicLinkCustomField))
                {
                    searchUrl = _jiraConfig.ApiUrl + jiraApiEpicLinkCustomField;
                    responseJson = GetJson(searchUrl);
                    var responseIssueObjectEpicTask = JsonConvert.DeserializeObject<Issue>(responseJson);
                    if (responseIssueObjectEpicTask != null)
                        projectShortName = CheckingAmbiguityOfProjectCodes(responseIssueObjectEpicTask);
                }
            }
            else if (issue.Fields.Parent == null && string.IsNullOrEmpty(issue.Fields.JiraPRCCustomField) && string.IsNullOrEmpty(issue.Fields.JiraEpicLinkCustomField))
            {
                //раскомментировать, когда потребуется определять код проекта по наименованию рабочей области Jira
                //projectShortName = issue.Fields.JiraProject.ProjectName;
            }

            return projectShortName;
        }

        public string GetProjectShortNameFromExternalWorkspace(Issue issue, string userJiraLogin, DateTime periodStartDate, DateTime periodEndDate)
        {
            var projectShortName = string.Empty;
            foreach (var projectExternalWorkspace in _projectExternalWorkspaceService.Get(p => p.Where(x => x.WorkspaceType == ExternalWorkspaceType.JIRA && x.ExternalWorkspaceProjectShortName == issue.Fields.JiraProject.ProjectKey).ToList()))
            {
                issue.Fields.WorkLogs = GetWorklogsByIssueId(issue.Id);
                foreach (var worklog in issue.Fields.WorkLogs)
                {
                    if (String.IsNullOrEmpty(worklog.Author.Name) == false && String.IsNullOrEmpty(userJiraLogin) == false && worklog.Author.Name.ToLower() == userJiraLogin.ToLower() &&
                        worklog.Started >= periodStartDate.StartOfDay() && worklog.Started <= periodEndDate.EndOfDay())
                    {
                        var recordDate = worklog.Started.Date;
                        if (recordDate >= projectExternalWorkspace.ExternalWorkspaceDateBegin && recordDate <= projectExternalWorkspace.ExternalWorkspaceDateEnd)
                        {
                            projectShortName = projectExternalWorkspace.Project.ShortName;
                            break;
                        }
                    }
                }
            }
            return projectShortName;
        }

        public bool IsExternalWorkspaceProjectShortName(string externalWorkspaceProjectShortName)
        {
            var searchUrl = _jiraConfig.ApiProject;
            var responseJson = GetJson(searchUrl);
            return JsonConvert.DeserializeObject<List<JiraProject>>(responseJson).Any(x => x.ProjectKey == externalWorkspaceProjectShortName);
        }

        public Project FindProjectFromExternalWorkspace(Issue issue, string userJiraLogin, DateTime periodStartDate, DateTime periodEndDate)
        {
            var project = new Project();
            foreach (var projectExternalWorkspace in _projectExternalWorkspaceService.Get(p => p.Where(x => x.WorkspaceType == ExternalWorkspaceType.JIRA && x.ExternalWorkspaceProjectShortName == issue.Fields.JiraProject.ProjectKey).ToList()))
            {
                issue.Fields.WorkLogs = GetWorklogsByIssueId(issue.Id);
                foreach (var worklog in issue.Fields.WorkLogs)
                {
                    if (String.IsNullOrEmpty(worklog.Author.Name) == false && String.IsNullOrEmpty(userJiraLogin) == false && worklog.Author.Name.ToLower() == userJiraLogin.ToLower() &&
                        worklog.Started >= periodStartDate && worklog.Started <= periodEndDate.EndOfDay())
                    {
                        var recordDate = worklog.Started.Date;
                        if (recordDate >= projectExternalWorkspace.ExternalWorkspaceDateBegin && recordDate <= projectExternalWorkspace.ExternalWorkspaceDateEnd)
                        {
                            project = projectExternalWorkspace.Project;
                        }
                    }
                }
            }
            return project;
        }

        public List<Worklog> GetWorklogsByIssueId(int issueId)
        {
            var searchUrl = _jiraConfig.ApiIssueId + issueId + "/worklog";
            var responseJson = GetJson(searchUrl);
            JObject jObject = JObject.Parse(responseJson);
            return jObject.SelectToken("worklogs").ToObject<List<Worklog>>();
        }

        public string CreateUrlWorklogsByUser(DateTime datesStartDate, DateTime dateEndDate)
        {
            var userJiraLogin = ADHelper.GetUserLoginWithoutDomainName(_userService.GetCurrentUser().UserLogin);
            var searchUrl = "";
            if (_jiraConfig.ApiUrl != null)
            {
                searchUrl = _jiraConfig.ApiUrl + "/latest/search?jql=worklogAuthor='" + userJiraLogin + "' and worklogDate >= '"
                            + datesStartDate.ToString("yyyy/MM/dd", new CultureInfo("en-US")) + "'" +
                            " and worklogDate <= '" + dateEndDate.ToString("yyyy/MM/dd", new CultureInfo("en-US")) + "'" + "&fields=project&fields=parent&fields=worklog&fields=customfield_11200&fields=customfield_11700&fields=customfield_12013&fields=customfield_12700&startAt=0&maxResults=10000";
            }
            return searchUrl;
        }

        public string CreateUrlWorklogsByUser(DateTime datesStartDate, DateTime dateEndDate, string user)
        {
            var userJiraLogin = ADHelper.GetUserLoginWithoutDomainName(user);
            var searchUrl = "";
            if (_jiraConfig.ApiUrl != null)
            {
                searchUrl = _jiraConfig.ApiUrl + "/latest/search?jql=worklogAuthor='" + userJiraLogin + "' and worklogDate >= '"
                            + datesStartDate.ToString("yyyy/MM/dd", new CultureInfo("en-US")) + "'" +
                            " and worklogDate <= '" + dateEndDate.ToString("yyyy/MM/dd", new CultureInfo("en-US")) + "'" + "&fields=project&fields=parent&fields=worklog&fields=customfield_11200&fields=customfield_11700&fields=customfield_12013&fields=customfield_12700&startAt=0&maxResults=10000";
            }
            return searchUrl;
        }
    }
}
