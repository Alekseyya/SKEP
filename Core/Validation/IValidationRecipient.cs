namespace Core.Validation
{
    public interface IValidationRecipient
    {
        void SetError(string fieldName, string errorMessage);
    }
}
