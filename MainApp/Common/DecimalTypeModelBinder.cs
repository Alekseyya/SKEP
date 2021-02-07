using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System.Threading.Tasks;


namespace MainApp.Common
{
    public class InvariantDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (!context.Metadata.IsComplexType && (context.Metadata.ModelType == typeof(double) || context.Metadata.ModelType == typeof(double?)))
            {
                return new InvariantDecimalModelBinder(context.Metadata.ModelType);
            }

            return null;
        }
    }

    public class InvariantDecimalModelBinder : IModelBinder
    {
        private readonly SimpleTypeModelBinder _baseBinder;

        public InvariantDecimalModelBinder(Type modelType)
        {
            _baseBinder = new SimpleTypeModelBinder(modelType);
        }
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

                var valueAsString = valueProviderResult.FirstValue;

                if (decimal.TryParse(valueAsString.Replace(" ", "").Replace(".", ","), out var result))
                {
                    bindingContext.Result = ModelBindingResult.Success(result);
                    return Task.CompletedTask;
                }

            }
            return _baseBinder.BindModelAsync(bindingContext);
        }

    }
}