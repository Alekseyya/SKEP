using System;

namespace Core.Validation
{
    public abstract class EntityValidatorBase<TEntity>
    {
        private readonly TEntity _entity;

        protected TEntity Entity
        {
            get { return _entity; }
        }

        private readonly IValidationRecipient _recipient;

        protected IValidationRecipient Recipient
        {
            get { return _recipient; }
        }

        protected EntityValidatorBase(TEntity entity, IValidationRecipient recipient)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (recipient == null)
                throw new ArgumentNullException(nameof(recipient));

            _entity = entity;
            _recipient = recipient;
        }

        public abstract void Validate();
    }
}
