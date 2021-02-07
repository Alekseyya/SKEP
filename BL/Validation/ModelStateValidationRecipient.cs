using System;
using Core.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BL.Validation
{
    public class ModelStateValidationRecipient : IValidationRecipient
    {
        private readonly ModelStateDictionary _modelStateDictionary;

        protected ModelStateDictionary ModelStateDictionary
        {
            get { return _modelStateDictionary; }
        }

        public ModelStateValidationRecipient(ModelStateDictionary modelStateDictionary)
        {
            if (modelStateDictionary == null)
                throw new ArgumentNullException(nameof(modelStateDictionary));

            _modelStateDictionary = modelStateDictionary;
        }

        public void SetError(string fieldName, string errorMessage)
        {
            ModelStateDictionary.AddModelError(fieldName, errorMessage);
        }
    }
}