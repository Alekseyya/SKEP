using System;


namespace Core.Data
{
    public class DataException : Exception
    {
        public DataException()
        { }

        public DataException(string message) : base(message)
        { }

        public DataException(string message, Exception innerException) : base(message, innerException)
        { }
    }

    public class EntityNotFoundException : DataException
    {
        public EntityNotFoundException()
            : base("Сущность не найдена")
        { }

        public EntityNotFoundException(string entityName)
            : base($"Сущность {entityName} с указанным ID не найдена")
        { }

        public EntityNotFoundException(string entityName, object entityId)
            : base($"Сущность {entityName} с ID {entityId} не найдена")
        { }

        public EntityNotFoundException(Exception innerException)
            : base("Сущность не найдена", innerException)
        { }

        public EntityNotFoundException(string entityName, Exception innerException)
            : base($"Сущность {entityName} с указанным ID не найдена", innerException)
        { }

        public EntityNotFoundException(string entityName, object entityId, Exception innerException)
            : base($"Сущность {entityName} с ID {entityId} не найдена", innerException)
        { }
    }

    public class EntityNotDeletedException : DataException
    {
        public EntityNotDeletedException()
            : base("Сущность не является удаленной")
        { }

        public EntityNotDeletedException(string entityName)
            : base($"Сущность {entityName} с указанным ID не является удаленной")
        { }

        public EntityNotDeletedException(string entityName, object entityId)
            : base($"Сущность {entityName} с ID {entityId} не является удаленной")
        { }

        public EntityNotDeletedException(Exception innerException)
            : base("Сущность не является удаленной", innerException)
        { }

        public EntityNotDeletedException(string entityName, Exception innerException)
            : base($"Сущность {entityName} с указанным ID не является удаленной", innerException)
        { }

        public EntityNotDeletedException(string entityName, object entityId, Exception innerException)
            : base($"Сущность {entityName} с ID {entityId} не является удаленной", innerException)
        { }
    }

    public class RepositoryNotFoundException : DataException
    {
        public RepositoryNotFoundException()
            : base("Репозиторий с указанным типом не найден")
        { }

        public RepositoryNotFoundException(Type repositoryType)
            : base($"Репозиторий с типом {repositoryType.FullName} не найден")
        { }

        public RepositoryNotFoundException(Exception innerException)
            : base("Репозиторий с указанным типом не найден", innerException)
        { }

        public RepositoryNotFoundException(Type repositoryType, Exception innerException)
            : base($"Репозиторий с типом {repositoryType.FullName} не найден", innerException)
        { }
    }
}
