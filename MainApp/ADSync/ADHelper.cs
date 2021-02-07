using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Web;

namespace MainApp.ADSync
{
    //public class ADHelper
    //{
    //    public static Principal GetUserADPrincipalByLogin(string userLogin)
    //    {
    //        UserPrincipal userPrincipal = null;

    //        try
    //        {
    //            string domainName = userLogin.Split('\\')[0];
    //            using (var pc = new PrincipalContext(ContextType.Domain, domainName))
    //            {
    //                userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, userLogin);
    //            }
    //        }
    //        catch (Exception)
    //        {
    //            userPrincipal = null;
    //        }

    //        return userPrincipal;
    //    }
    //    public static string GetUserTitleByLogin(string userLogin)
    //    {
    //        string userTitle = "";

    //        try
    //        {
    //            string domainName = userLogin.Split('\\')[0];
    //            using (var pc = new PrincipalContext(ContextType.Domain, domainName))
    //            {
    //                UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, userLogin);

    //                if (userPrincipal != null)
    //                {
    //                    userTitle = userPrincipal.Name;
    //                }
    //            }
    //        }
    //        catch (Exception)
    //        {
    //            userTitle = userLogin;
    //        }

    //        return userTitle;
    //    }

    //    public static string GetUserLoginByTitle(string userTitle)
    //    {
    //        string userLogin = "";
    //        string domainName = Domain.GetCurrentDomain().Name;

    //        using (var pc = new PrincipalContext(ContextType.Domain, domainName))
    //        {
    //            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.Name, userTitle);

    //            if (userPrincipal != null)
    //            {
    //                userLogin = userPrincipal.SamAccountName;
    //            }
    //        }

    //        return userLogin;
    //    }

    //    public static string GetADEmployeeIDBySearchString(RPCSContext db, string searchString)
    //    {
    //        string userADEmployeeID = "";

    //        var employees = db.Employees;

    //        string[] searchTokens = searchString.Split(' ');

    //        List<string> searchTokensList = new List<string>();
    //        for (int i = 0; i < searchTokens.Length; i++)
    //        {
    //            if (String.IsNullOrEmpty(searchTokens[i]) == false
    //                && String.IsNullOrEmpty(searchTokens[i].Trim()) == false)
    //            {
    //                searchTokensList.Add(searchTokens[i].Trim());
    //            }
    //        }

    //        List<Employee> employeeList = null;

    //        if (searchTokensList.Count > 1)
    //        {
    //            employeeList = employees.Where(e => searchTokensList.All(stl => e.FirstName.Contains(stl)
    //                                   || e.LastName.Contains(stl)
    //                                   || e.MidName.Contains(stl)
    //                                   || e.Email.Contains(stl))).ToList();
    //        }
    //        else
    //        {
    //            employeeList = employees.Where(e => e.FirstName.Contains(searchString.Trim())
    //                                   || e.LastName.Contains(searchString.Trim())
    //                                   || e.MidName.Contains(searchString.Trim())
    //                                   || e.Email.Contains(searchString.Trim())).ToList();
    //        }

    //        if (employeeList != null && employeeList.Count() == 1
    //            && String.IsNullOrEmpty(employeeList.FirstOrDefault().ADEmployeeID) == false)
    //        {
    //            userADEmployeeID = employeeList.FirstOrDefault().ADEmployeeID;
    //        }
    //        else
    //        {
    //            userADEmployeeID = GetADEmployeeIDBySearchStringInAD(searchString);
    //        }

    //        return userADEmployeeID;
    //    }

    //    public static string GetADEmployeeIDBySearchStringInAD(string searchString)
    //    {
    //        string userADEmployeeID = "";
    //        string domainName = Domain.GetCurrentDomain().Name;

    //        using (var pc = new PrincipalContext(ContextType.Domain, domainName))
    //        {
    //            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.Name, searchString);

    //            if (userPrincipal != null)
    //            {
    //                DirectoryEntry de = (userPrincipal.GetUnderlyingObject() as DirectoryEntry);

    //                if (de.Properties.Contains("extensionAttribute10") == true
    //                    && de.Properties["extensionAttribute10"].Value != null)
    //                {
    //                    userADEmployeeID = de.Properties["extensionAttribute10"].Value.ToString();
    //                }
    //            }
    //        }

    //        return userADEmployeeID;
    //    }

    //    public static string GetADEmployeeIDByEmployeeIDInDB(RPCSContext db, int id)
    //    {
    //        string userADEmployeeID = "";

    //        Employee employee = db.Employees.Find(id);

    //        if (employee != null)
    //        {
    //            if (String.IsNullOrEmpty(employee.ADEmployeeID) == false)
    //            {
    //                userADEmployeeID = employee.ADEmployeeID;
    //            }
    //            else
    //            {
    //                userADEmployeeID = GetADEmployeeIDBySearchStringInAD(NormalizeAndTrimString(employee.FullName));
    //            }
    //        }

    //        return userADEmployeeID;
    //    }

    //    public static string GetEmployeeTitleByADEmployeeID(RPCSContext db, string userADEmployeeID)
    //    {
    //        string employeeTitle = "";

    //        Employee employee = db.Employees.Where(e => e.ADEmployeeID == userADEmployeeID).FirstOrDefault();

    //        if (employee != null)
    //        {
    //            employeeTitle = employee.FullName;
    //        }

    //        return employeeTitle;
    //    }

    //    public static string GetDomainNetbiosName(Domain domain)
    //    {

    //        // Bind to RootDSE and grab the configuration naming context
    //        DirectoryEntry rootDSE = new DirectoryEntry(@"LDAP://RootDSE");
    //        DirectoryEntry partitions = new DirectoryEntry(@"LDAP://cn=Partitions," + rootDSE.Properties["configurationNamingContext"].Value);

    //        DirectoryEntry domainEntry = domain.GetDirectoryEntry();

    //        //Iterate through the cross references collection in the Partitions container
    //        DirectorySearcher directorySearcher = new DirectorySearcher(partitions)
    //        {
    //            Filter = "(&(objectCategory=crossRef)(ncName=" + domainEntry.Path .Replace("LDAP://", string.Empty).Replace(domain.Name + "/", string.Empty) + "))",
    //            SearchScope = SearchScope.Subtree
    //        };

    //        directorySearcher.PropertiesToLoad.Add("nETBIOSName");

    //        //Display result (should only be one)
    //        SearchResultCollection results = directorySearcher.FindAll();
    //        if (results.Count == 0)
    //        {
    //            return null;
    //        }

    //        return results[0].Properties["nETBIOSName"][0].ToString();
    //    }

    //    public static string NormalizeAndTrimString(string s)
    //    {
    //        if (s != null)
    //        {
    //            s = s.Replace("\r", "").Replace("\n", " ").Replace("\t", " ").Replace("\u00A0", " ").Trim();
    //        }

    //        return s;
    //    }
    //}

}