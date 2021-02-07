


using Core.Models;

namespace Core.Data.Interfaces
{
    public interface IEmployeeRepository : IRepository<Employee, int>
    {
        Employee GetByLogin(string login);
    }
}
