using System.Collections.Generic;
using Core.Models;


namespace Core.BL.Interfaces
{
    public interface IUserService: IServiceBase<RPCSUser, int>
    {
        IList<RPCSUser> GetList();
        RPCSUser GetUserByLogin(string userLogin);
        RPCSUser GetCurrentUser();
        Employee GetEmployeeForCurrentUser();
        (string, string) GetUserDataForVersion();
    }
}
