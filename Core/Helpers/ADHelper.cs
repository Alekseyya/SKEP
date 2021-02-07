using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;

namespace Core.Helpers
{
    public class ADHelper
    {
        public static Principal GetUserADPrincipalByLogin(string userLogin)
        {
            UserPrincipal userPrincipal = null;

            try
            {
                string domainName = userLogin.Split('\\')[0];
                using (var pc = new PrincipalContext(ContextType.Domain, domainName))
                {
                    userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, userLogin);
                }
            }
            catch (Exception)
            {
                userPrincipal = null;
            }

            return userPrincipal;
        }
        public static IEnumerable<string> GetUserLoginsByName(string userName)
        {
            IEnumerable<string> users = null;
            try
            {
                string domainName = Domain.GetCurrentDomain().Name;
                using (var ctx = new PrincipalContext(ContextType.Domain, domainName))
                {
                    var userPrincipal = new UserPrincipal(ctx);
                    userPrincipal.Name = "*" + userName + "*";
                    using (var searcher = new PrincipalSearcher())
                    {
                        searcher.QueryFilter = userPrincipal;
                        var allUsers = searcher.FindAll();
                        var first = allUsers.ElementAt(0);
                        users = allUsers.Select(u => u.SamAccountName).ToList();
                    }
                }
            }
            catch (Exception)
            {

            }
            return users;
        }

        public static string GetUserTitleByLogin(string userLogin)
        {
            string userTitle = "";

            try
            {
                string domainName = userLogin.Split('\\')[0];
                using (var pc = new PrincipalContext(ContextType.Domain, domainName))
                {
                    UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, userLogin);

                    if (userPrincipal != null)
                    {
                        userTitle = userPrincipal.Name;
                    }
                }
            }
            catch (Exception)
            {
                userTitle = userLogin;
            }

            return userTitle;
        }

        public static string GetUserLoginByTitle(string userTitle)
        {
            string userLogin = "";
            string domainName = Domain.GetCurrentDomain().Name;

            using (var pc = new PrincipalContext(ContextType.Domain, domainName))
            {
                UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.Name, userTitle);

                if (userPrincipal != null)
                {
                    userLogin = userPrincipal.SamAccountName;
                }
            }

            return userLogin;
        }

        public static string GetADEmployeeIDBySearchStringInAD(string searchString)
        {
            string userADEmployeeID = "";
            string domainName = Domain.GetCurrentDomain().Name;

            using (var pc = new PrincipalContext(ContextType.Domain, domainName))
            {
                UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.Name, searchString);

                if (userPrincipal != null)
                {
                    DirectoryEntry de = (userPrincipal.GetUnderlyingObject() as DirectoryEntry);

                    if (de.Properties.Contains("extensionAttribute10") == true
                        && de.Properties["extensionAttribute10"].Value != null)
                    {
                        userADEmployeeID = de.Properties["extensionAttribute10"].Value.ToString();
                    }
                }
            }

            return userADEmployeeID;
        }

        public static string GetDomainNetbiosName(Domain domain)
        {

            // Bind to RootDSE and grab the configuration naming context
            DirectoryEntry rootDSE = new DirectoryEntry(@"LDAP://RootDSE");
            DirectoryEntry partitions = new DirectoryEntry(@"LDAP://cn=Partitions," + rootDSE.Properties["configurationNamingContext"].Value);

            DirectoryEntry domainEntry = domain.GetDirectoryEntry();

            //Iterate through the cross references collection in the Partitions container
            DirectorySearcher directorySearcher = new DirectorySearcher(partitions)
            {
                Filter = "(&(objectCategory=crossRef)(ncName=" + domainEntry.Path.Replace("LDAP://", string.Empty).Replace(domain.Name + "/", string.Empty) + "))",
                SearchScope = SearchScope.Subtree
            };

            directorySearcher.PropertiesToLoad.Add("nETBIOSName");

            //Display result (should only be one)
            SearchResultCollection results = directorySearcher.FindAll();
            if (results.Count == 0)
            {
                return null;
            }

            return results[0].Properties["nETBIOSName"][0].ToString();
        }

        public static string GetUserLoginWithoutDomainName(string userLogin)
        {
            string userLoginWithoutDomainName = "";
            if (userLogin.Contains("\\") == true)
            {
                userLoginWithoutDomainName = userLogin.Split('\\')[1];
            }
            else
            {
                userLoginWithoutDomainName = userLogin;
            }

            return userLoginWithoutDomainName;
        }
    }
}
