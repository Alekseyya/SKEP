using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using Core.BL.Interfaces;
using Core.Common;
using Core.Config;
using Core.Helpers;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace MainApp.ADSync
{
    public class SyncWithADTask : LongRunningTaskBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ADConfig _adConfig;

        List<string> adContainerList = new List<string>();

        public Tuple<string, string> SyncAdCredentials;

        public SyncWithADTask(IEmployeeService employeeService, IOptions<ADConfig> adOptions)
            : base()
        {
            _employeeService = employeeService;
            _adConfig = adOptions.Value;
        }

        private void InitSyncAdUser()
        {
            string user = "", password = "";
            if (!string.IsNullOrEmpty(_adConfig.SyncUser))
                user = _adConfig.SyncUser;

            if (!string.IsNullOrEmpty(_adConfig.SyncPassword))
                password = _adConfig.SyncPassword;

            SyncAdCredentials = new Tuple<string, string>(user, password);

            if (!string.IsNullOrEmpty(_adConfig.SyncContainers))
            {
                string syncADContainers = _adConfig.SyncContainers;

                string[] containers = syncADContainers.Split('|');
                foreach (string container in containers)
                {
                    adContainerList.Add(container);
                }
            }
        }

        public ADSyncResult ProcessLongRunningAction(string userIdentityName,
            string id,
            bool saveDataInAD)
        {
            AdSyncReport report = null;

            InitSyncAdUser();
            //TODO - try- catсh, ошибку в логи
            taskId = id;

            try
            {
                SetStatus(0, "Старт синхронизации...");

                report = SyncEmployeesWithAD(saveDataInAD);

                SetStatus(100, "Синхронизация завершена");
            }
            catch (Exception e)
            {
                SetStatus(-1, "Ошибка: " + e.Message);
            }

            string htmlReport = "Нет данных отчета";

            if (report != null)
            {
                htmlReport = report.GenerateHtmlReport();
            }

            return new ADSyncResult() { fileId = id, fileHtmlReport = htmlReport };
        }

        private bool IsDirectoryEntryPropertyValueEquals(DirectoryEntry de, string propertyName, object value)
        {
            bool result = false;

            if (value != null
                && de.Properties.Contains(propertyName) == true
                && de.Properties[propertyName] != null
                && de.Properties[propertyName].Value != null
                && de.Properties[propertyName].Value.ToString().Trim().Equals(value.ToString().Trim()) == true)
            {
                result = true;
            }

            return result;
        }

        private string GetDirectoryEntryPropertyDisplayValue(string value)
        {
            string result = "";

            if (String.IsNullOrEmpty(value) == false)
            {
                result = value;

                if (result.Contains("CN=") == true
                    && result.Contains(",") == true)
                {
                    result = result.Split(',')[0].Replace("CN=", "");
                }
            }

            return result;
        }

        private string GetDirectoryEntryPropertyChangeValueInfo(DirectoryEntry de, string propertyName, object value)
        {
            string result = "";

            if (value != null)
            {
                if (de.Properties.Contains(propertyName) == true
                    && de.Properties[propertyName] != null
                    && de.Properties[propertyName].Value != null)
                {
                    result = GetDirectoryEntryPropertyDisplayValue(de.Properties[propertyName].Value.ToString())
                        + " -> " + GetDirectoryEntryPropertyDisplayValue(value.ToString());
                }
                else
                {
                    result = "... -> " + GetDirectoryEntryPropertyDisplayValue(value.ToString());
                }
            }

            return result;
        }

        private string GetDirectoryEntryPropertyNoChangeValueInfo(DirectoryEntry de, string propertyName)
        {
            string result = "";

            if (de.Properties.Contains(propertyName) == true
                && de.Properties[propertyName] != null
                && de.Properties[propertyName].Value != null)
            {
                result = GetDirectoryEntryPropertyDisplayValue(de.Properties[propertyName].Value.ToString());
            }

            return result;
        }

        private string GetDepartmentFullNameForAD(Department department)
        {
            string departmentFullName = "";

            string shortName = department.ShortName;
            string title = department.Title;

            if (String.IsNullOrEmpty(shortName) == false
                && String.IsNullOrEmpty(shortName.Trim()) == false)
            {
                if (shortName.Contains("-") == true)
                {
                    departmentFullName += shortName.Substring(0, shortName.IndexOf("-")).Trim() + " ";
                }
                else
                {
                    departmentFullName += shortName.Trim() + " ";
                }
            }

            departmentFullName += title.Trim();

            return departmentFullName;
        }



        private AdSyncReport SyncEmployeesWithAD(bool saveDataInAD)
        {
            AdSyncReport report = new AdSyncReport();

            string domainName = Domain.GetCurrentDomain().Name;
            string domainNetbiosName = ADHelper.GetDomainNetbiosName(Domain.GetCurrentDomain());

            List<Employee> employeeList = _employeeService.Get(x => x
                .Where(e => (e.DismissalDate == null || e.DismissalDate >= DateTime.Today) && e.IsVacancy == false)
                .Include(e => e.EmployeePosition)
                .Include(e => e.Department)
                .Include(e => e.Organisation)
                .Where(e => e.Department != null).ToList()
                .OrderBy(e => e.FullName).ToList()).ToList();
            var totalCount = employeeList.Count;

            int k = 0;
            foreach (var employee in employeeList)
            {
                SetStatus(90 * k / totalCount, "Синхронизация для сотрудника: " + employee.FullName);

                bool employeeFoundInAD = false;

                using (var pcAD = new PrincipalContext(ContextType.Domain, domainName))
                {
                    foreach (string adContainer in adContainerList)
                    {
                        using (var pcContainer = new PrincipalContext(ContextType.Domain, domainName, adContainer))
                        {
                            bool isEmployeeChanged = false;

                            var AD_User = GetEmployeeFromAD(pcContainer, employee, domainNetbiosName);

                            if (AD_User != null)
                            {
                                employeeFoundInAD = true;

                                bool isADUserChanged = false;

                                string adEmployeeTitleChangedInfo = "";
                                string adEmployeeDepartmentChangedInfo = "";
                                string adEmployeeManagerChangedInfo = "";

                                string adEmployeeOrganisationTitleChangedInfo = "";
                                string adEmployeeOfficeNameChangedInfo = "";
                                string adEmployeeWorkPhoneNumberChangedInfo = "";
                                string adEmployeePublicMobilePhoneNumberChangedInfo = "";
                                string adEmployeeEmployeeLocationTitleChangedInfo = "";

                                DirectoryEntry de = (AD_User.GetUnderlyingObject() as DirectoryEntry);
                                de.Username = SyncAdCredentials.Item1;
                                de.Password = SyncAdCredentials.Item2;

                                if (String.IsNullOrEmpty(employee.ADLogin))
                                {
                                    if (String.IsNullOrEmpty(domainNetbiosName) == false)
                                    {
                                        employee.ADLogin = domainNetbiosName + "\\" + AD_User.SamAccountName;
                                    }
                                    else
                                    {
                                        employee.ADLogin = AD_User.SamAccountName;
                                    }

                                    report.NewUsers.SafeAddToList(new ADSyncEmployeeInfo(employee,
                                        "Получен ADLogin в из AD"));

                                    isEmployeeChanged = true;
                                }

                                if (String.IsNullOrEmpty(employee.ADEmployeeID))
                                {
                                    if (de.Properties.Contains("extensionAttribute10") == true
                                        && de.Properties["extensionAttribute10"] != null
                                        && de.Properties["extensionAttribute10"].Value != null
                                        && String.IsNullOrEmpty(de.Properties["extensionAttribute10"].Value.ToString()) == false)
                                    {
                                        employee.ADEmployeeID = Convert.ToString(de.Properties["extensionAttribute10"].Value);

                                        report.NewUsers.SafeAddToList(new ADSyncEmployeeInfo(employee,
                                            "Получен EmployeeID в из AD"));

                                        isEmployeeChanged = true;
                                    }
                                }

                                if (String.IsNullOrEmpty(employee.Email)
                                    || (String.IsNullOrEmpty(AD_User.EmailAddress) == false && employee.Email.Equals(AD_User.EmailAddress) == false))
                                {
                                    if (AD_User.EmailAddress != null)
                                    {
                                        employee.Email = AD_User.EmailAddress;

                                        report.NewUsers.SafeAddToList(new ADSyncEmployeeInfo(employee,
                                            "Получен E-mail в из AD"));

                                        isEmployeeChanged = true;
                                    }
                                }

                                //--------------------------------
                                //Должность
                                if (employee.EmployeePositionTitle != null && !String.IsNullOrEmpty(employee.EmployeePositionTitle)
                                    && IsDirectoryEntryPropertyValueEquals(de, "title", employee.EmployeePositionTitle) == false)
                                {
                                    adEmployeeTitleChangedInfo = GetDirectoryEntryPropertyChangeValueInfo(de, "title", employee.EmployeePositionTitle.Trim());
                                    de.Properties["title"].Value = Convert.ToString(employee.EmployeePositionTitle.Trim());

                                    isADUserChanged = true;
                                }
                                if (String.IsNullOrEmpty(adEmployeeTitleChangedInfo) == true)
                                {
                                    adEmployeeTitleChangedInfo = GetDirectoryEntryPropertyNoChangeValueInfo(de, "title");
                                }

                                //отдел
                                if (employee.Department != null && !String.IsNullOrEmpty(employee.Department.Title)
                                     && IsDirectoryEntryPropertyValueEquals(de, "department", GetDepartmentFullNameForAD(employee.Department)) == false)
                                {
                                    adEmployeeDepartmentChangedInfo = GetDirectoryEntryPropertyChangeValueInfo(de, "department", GetDepartmentFullNameForAD(employee.Department));
                                    de.Properties["department"].Value = Convert.ToString(GetDepartmentFullNameForAD(employee.Department));

                                    isADUserChanged = true;
                                }
                                if (String.IsNullOrEmpty(adEmployeeDepartmentChangedInfo) == true)
                                {
                                    adEmployeeDepartmentChangedInfo = GetDirectoryEntryPropertyNoChangeValueInfo(de, "department");
                                }

                                //--------------------------------
                                //Руководитель
                                if (employee.Department != null)
                                {
                                    UserPrincipal managerAD = null;
                                    string managerFullName = "";
                                    if (employee.Department.DepartmentManager != null
                                        && employee.ID != employee.Department.DepartmentManagerID)
                                    {
                                        managerFullName = employee.Department.DepartmentManager.FullName;
                                        managerAD = GetEmployeeFromAD(pcAD, employee.Department.DepartmentManager, domainNetbiosName);
                                    }
                                    else if (employee.Department.ParentDepartment != null
                                        && employee.Department.ParentDepartment.DepartmentManager != null
                                        && employee.ID != employee.Department.ParentDepartment.DepartmentManagerID)
                                    {
                                        managerFullName = employee.Department.ParentDepartment.DepartmentManager.FullName;
                                        managerAD = GetEmployeeFromAD(pcAD, employee.Department.ParentDepartment.DepartmentManager, domainNetbiosName);
                                    }
                                    if (managerAD != null)
                                    {
                                        var managerDE = (DirectoryEntry)managerAD.GetUnderlyingObject();
                                        var managerDN = managerDE.Properties["distinguishedName"][0];
                                        if (IsDirectoryEntryPropertyValueEquals(de, "manager", managerDN) == false)
                                        {
                                            adEmployeeManagerChangedInfo = GetDirectoryEntryPropertyChangeValueInfo(de, "manager", managerDN);
                                            de.Properties["manager"].Value = managerDN;

                                            isADUserChanged = true;
                                        }
                                    }
                                    else if (String.IsNullOrEmpty(managerFullName) == false)
                                    {
                                        adEmployeeManagerChangedInfo = "Не найти в AD учетную запись руководителя: " + managerFullName;
                                    }
                                    else
                                    {
                                        adEmployeeManagerChangedInfo = "Не удалось определить руководителя по данным";
                                    }
                                }

                                if (String.IsNullOrEmpty(adEmployeeManagerChangedInfo) == true)
                                {
                                    adEmployeeManagerChangedInfo = GetDirectoryEntryPropertyNoChangeValueInfo(de, "manager");
                                }

                                //--------------------------------
                                //Организация
                                if (employee.Organisation != null && !String.IsNullOrEmpty(employee.Organisation.Title)
                                     && IsDirectoryEntryPropertyValueEquals(de, "company", employee.Organisation.Title.Trim()) == false)
                                {
                                    adEmployeeOrganisationTitleChangedInfo = GetDirectoryEntryPropertyChangeValueInfo(de, "company", employee.Organisation.Title.Trim());
                                    de.Properties["company"].Value = Convert.ToString(employee.Organisation.Title.Trim());

                                    isADUserChanged = true;
                                }
                                if (String.IsNullOrEmpty(adEmployeeOrganisationTitleChangedInfo) == true)
                                {
                                    adEmployeeOrganisationTitleChangedInfo = GetDirectoryEntryPropertyNoChangeValueInfo(de, "company");
                                }

                                //--------------------------------
                                //Офис (№ кабинета)
                                if (employee.OfficeName != null && !String.IsNullOrEmpty(employee.OfficeName)
                                     && IsDirectoryEntryPropertyValueEquals(de, "physicalDeliveryOfficeName", employee.OfficeName.Trim()) == false)
                                {
                                    adEmployeeOfficeNameChangedInfo = GetDirectoryEntryPropertyChangeValueInfo(de, "physicalDeliveryOfficeName", employee.OfficeName.Trim());
                                    de.Properties["physicalDeliveryOfficeName"].Value = Convert.ToString(employee.OfficeName.Trim());

                                    isADUserChanged = true;
                                }
                                if (String.IsNullOrEmpty(adEmployeeOfficeNameChangedInfo) == true)
                                {
                                    adEmployeeOfficeNameChangedInfo = GetDirectoryEntryPropertyNoChangeValueInfo(de, "physicalDeliveryOfficeName");
                                }

                                //--------------------------------
                                //Рабочий телефон
                                if (employee.WorkPhoneNumber != null && !String.IsNullOrEmpty(employee.WorkPhoneNumber)
                                     && IsDirectoryEntryPropertyValueEquals(de, "telephoneNumber", employee.WorkPhoneNumber.Trim()) == false)
                                {
                                    adEmployeeWorkPhoneNumberChangedInfo = GetDirectoryEntryPropertyChangeValueInfo(de, "telephoneNumber", employee.WorkPhoneNumber.Trim());
                                    de.Properties["telephoneNumber"].Value = Convert.ToString(employee.WorkPhoneNumber.Trim());

                                    isADUserChanged = true;
                                }
                                if (String.IsNullOrEmpty(adEmployeeWorkPhoneNumberChangedInfo) == true)
                                {
                                    adEmployeeWorkPhoneNumberChangedInfo = GetDirectoryEntryPropertyNoChangeValueInfo(de, "telephoneNumber");
                                }

                                //--------------------------------
                                //Мобильный телефон (общедоступный)
                                if (employee.PublicMobilePhoneNumber != null && !String.IsNullOrEmpty(employee.PublicMobilePhoneNumber)
                                     && IsDirectoryEntryPropertyValueEquals(de, "mobile", employee.PublicMobilePhoneNumber.Trim()) == false)
                                {
                                    adEmployeePublicMobilePhoneNumberChangedInfo = GetDirectoryEntryPropertyChangeValueInfo(de, "mobile", employee.PublicMobilePhoneNumber.Trim());
                                    de.Properties["mobile"].Value = Convert.ToString(employee.PublicMobilePhoneNumber.Trim());

                                    isADUserChanged = true;
                                }
                                if (String.IsNullOrEmpty(adEmployeePublicMobilePhoneNumberChangedInfo) == true)
                                {
                                    adEmployeePublicMobilePhoneNumberChangedInfo = GetDirectoryEntryPropertyNoChangeValueInfo(de, "mobile");
                                }

                                //--------------------------------
                                //Территориальное расположение
                                if (employee.EmployeeLocation != null && !String.IsNullOrEmpty(employee.EmployeeLocation.Title)
                                     && IsDirectoryEntryPropertyValueEquals(de, "l", employee.EmployeeLocation.Title.Trim()) == false)
                                {
                                    adEmployeeEmployeeLocationTitleChangedInfo = GetDirectoryEntryPropertyChangeValueInfo(de, "l", employee.EmployeeLocation.Title.Trim());
                                    de.Properties["l"].Value = Convert.ToString(employee.EmployeeLocation.Title.Trim());

                                    isADUserChanged = true;
                                }
                                if (String.IsNullOrEmpty(adEmployeeEmployeeLocationTitleChangedInfo) == true)
                                {
                                    adEmployeeEmployeeLocationTitleChangedInfo = GetDirectoryEntryPropertyNoChangeValueInfo(de, "l");
                                }


                                if (isADUserChanged)
                                {
                                    if (saveDataInAD == true)
                                    {
                                        string adSyncStatusMessage = "";

                                        try
                                        {
                                            de.CommitChanges();
                                            adSyncStatusMessage = "Изменено в AD";
                                        }
                                        catch (Exception e)
                                        {
                                            adSyncStatusMessage = "Ошибка при записи в AD: "
                                                + ((e != null && e.Message != null) ? e.Message : "");
                                        }

                                        report.UpdatedUsers.SafeAddToList(new ADSyncEmployeeInfo(employee,
                                            adEmployeeTitleChangedInfo,
                                            adEmployeeDepartmentChangedInfo,
                                            adEmployeeManagerChangedInfo,
                                            adEmployeeOrganisationTitleChangedInfo,
                                            adEmployeeOfficeNameChangedInfo,
                                            adEmployeeWorkPhoneNumberChangedInfo,
                                            adEmployeePublicMobilePhoneNumberChangedInfo,
                                            adEmployeeEmployeeLocationTitleChangedInfo,
                                            adSyncStatusMessage));
                                    }
                                    else
                                    {
                                        report.UpdatedUsers.SafeAddToList(new ADSyncEmployeeInfo(employee,
                                            adEmployeeTitleChangedInfo,
                                            adEmployeeDepartmentChangedInfo,
                                            adEmployeeManagerChangedInfo,
                                            adEmployeeOrganisationTitleChangedInfo,
                                            adEmployeeOfficeNameChangedInfo,
                                            adEmployeeWorkPhoneNumberChangedInfo,
                                            adEmployeePublicMobilePhoneNumberChangedInfo,
                                            adEmployeeEmployeeLocationTitleChangedInfo,
                                            "Без изменения в AD"));
                                    }
                                }

                                if (isEmployeeChanged)
                                    _employeeService.Update(employee);
                            }
                        }

                        if (employeeFoundInAD == true)
                        {
                            break;
                        }
                    }
                }

                if (employeeFoundInAD == false)
                {
                    report.NotFoundInAD.SafeAddToList(new ADSyncEmployeeInfo(employee,
                        "Не найдено в AD"));
                }

                k++;
            }

            //db.SaveChanges();

            return report;
        }

        private UserPrincipal GetEmployeeFromAD(PrincipalContext pc, Employee employee, string domainNetbiosName)
        {
            UserPrincipal userPrincipal = null;

            if (!String.IsNullOrEmpty(employee.ADLogin))
            {
                try
                {
                    userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, employee.ADLogin.ToLower().Replace(domainNetbiosName + "\\", ""));
                }
                catch (Exception)
                {
                    userPrincipal = null;
                }
            }

            if (userPrincipal == null)
            {
                try
                {
                    userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.Name, employee.FullName);
                }
                catch (Exception)
                {
                    userPrincipal = null;
                }
            }
            if (userPrincipal == null
                && !String.IsNullOrEmpty(employee.Email)
                && employee.Email.Equals("nomail@nomail.com") == false)
            {
                try
                {
                    UserPrincipal qbeUser = new UserPrincipal(pc);
                    qbeUser.EmailAddress = employee.Email;

                    PrincipalSearcher srch = new PrincipalSearcher(qbeUser);

                    PrincipalSearchResult<Principal> srchResult = srch.FindAll();

                    if (srchResult != null && srchResult.Count() == 1)
                    {
                        userPrincipal = srch.FindOne() as UserPrincipal;
                    }
                }
                catch (Exception)
                {
                    userPrincipal = null;
                }
            }
            return userPrincipal;
        }

    }
}