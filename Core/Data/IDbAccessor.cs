using Microsoft.EntityFrameworkCore;

namespace Core.Data
{
    public interface IDbAccessor<TDbContext>
        where TDbContext : DbContext
    {
        TDbContext GetDbContext();
    }
}
