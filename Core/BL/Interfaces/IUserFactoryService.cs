using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using Core.Models;


namespace Core.BL.Interfaces
{
   public interface IUserFactoryService
   {
        ApplicationUser GetUser(IPrincipal contextUser);
        ApplicationUser GetCurrentUser();
    }
}
