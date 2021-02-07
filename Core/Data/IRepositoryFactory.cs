namespace Core.Data
{
    public interface IRepositoryFactory
    {
        IRepository GetRepository<IRepository>();
        void EnableDeletedFilter();
        void EnableVersionsFilter();
        void DisableDeletedFilter();
        void DisableVersionsFilter();
    }
}
