using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using Core.BL.Interfaces;
using Core.Common;
using Core.Helpers;


namespace MainApp.ADSync
{
    public class ImportDataFromADTask : LongRunningTaskBase
    {
        private readonly IEmployeeService _employeeService;

        public ImportDataFromADTask(IEmployeeService employeeService)
            : base()
        {
            _employeeService = employeeService;
        }


        public string ProcessLongRunningAction(string userIdentityName, string id)
        {
            taskId = id;

            SetStatus(0, "Старт импорта...");

            var employeeList = _employeeService.Get(x => x.ToList().OrderBy(e => e.FullName).ToList());


            string domainName = Domain.GetCurrentDomain().Name;
            string domainNetbiosName = ADHelper.GetDomainNetbiosName(Domain.GetCurrentDomain());

            int k = 0;
            foreach (var employee in employeeList)
            {
                SetStatus(k * 100 / employeeList.Count(), "Импорт данных из AD для сотрудника: " + employee.FullName);

                using (var pc = new PrincipalContext(ContextType.Domain, domainName))
                {
                    UserPrincipal userPrincipal = null;

                    try
                    {
                        try
                        {
                            userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.Name, employee.FullName);
                        }
                        catch (Exception)
                        {
                            userPrincipal = null;
                        }

                        if (userPrincipal == null
                            && String.IsNullOrEmpty(employee.ADLogin) == false)
                        {
                            try
                            {
                                userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, employee.ADLogin.Replace(domainNetbiosName + "\\", ""));
                            }
                            catch (Exception)
                            {
                                userPrincipal = null;
                            }
                        }

                        if (userPrincipal == null
                            && String.IsNullOrEmpty(employee.Email) == false)
                        {
                            try
                            {
                                UserPrincipal qbeUser = new UserPrincipal(pc);
                                qbeUser.EmailAddress = employee.Email;

                                PrincipalSearcher srch = new PrincipalSearcher(qbeUser);

                                userPrincipal = srch.FindOne() as UserPrincipal;
                            }
                            catch (Exception)
                            {
                                userPrincipal = null;
                            }
                        }

                        if (userPrincipal != null)
                        {
                            //db.Entry(employee).State = EntityState.Modified;

                            if (String.IsNullOrEmpty(domainNetbiosName) == false)
                            {
                                employee.ADLogin = domainNetbiosName + "\\" + userPrincipal.SamAccountName;
                            }
                            else
                            {
                                employee.ADLogin = userPrincipal.SamAccountName;
                            }

                            employee.Email = userPrincipal.EmailAddress;

                            DirectoryEntry de = (userPrincipal.GetUnderlyingObject() as DirectoryEntry);

                            if (de.Properties.Contains("physicalDeliveryOfficeName") == true
                                && de.Properties["physicalDeliveryOfficeName"].Value != null
                                && String.IsNullOrEmpty(de.Properties["physicalDeliveryOfficeName"].Value.ToString()) == false
                                && String.IsNullOrEmpty(employee.OfficeName) == true)
                            {
                                employee.OfficeName = de.Properties["physicalDeliveryOfficeName"].Value.ToString();
                            }

                            if (de.Properties.Contains("telephoneNumber") == true
                                && de.Properties["telephoneNumber"].Value != null
                                && String.IsNullOrEmpty(de.Properties["telephoneNumber"].Value.ToString()) == false
                                && String.IsNullOrEmpty(employee.WorkPhoneNumber) == true)
                            {
                                employee.WorkPhoneNumber = de.Properties["telephoneNumber"].Value.ToString();
                            }

                            if (de.Properties.Contains("mobile") == true
                                && de.Properties["mobile"].Value != null
                                && String.IsNullOrEmpty(de.Properties["mobile"].Value.ToString()) == false
                                && String.IsNullOrEmpty(employee.PublicMobilePhoneNumber) == true)
                            {
                                employee.PublicMobilePhoneNumber = de.Properties["mobile"].Value.ToString();
                            }

                            if (de.Properties.Contains("extensionAttribute10") == true
                                && de.Properties["extensionAttribute10"].Value != null
                                && String.IsNullOrEmpty(de.Properties["extensionAttribute10"].Value.ToString()) == false)
                            {
                                employee.ADEmployeeID = de.Properties["extensionAttribute10"].Value.ToString();
                            }
                            _employeeService.Update(employee);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                k++;
            }

            SetStatus(100, "Импорт завершен");

            return taskId;
        }
    }
}