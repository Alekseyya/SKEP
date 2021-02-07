using Core.Validation;

namespace Core.BL
{
    public interface IEntityValidatingService<TEntity>
    {
        void Validate(TEntity entity, IValidationRecipient validationRecipient);
    }
}
