using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;


namespace MainApp.Common
{

    public class InvariantDoubleModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (!context.Metadata.IsComplexType && (context.Metadata.ModelType == typeof(double) || context.Metadata.ModelType == typeof(double?)))
            {
                return new InvariantDoubleModelBinder(context.Metadata.ModelType);
            }

            return null;
        }
    }


    public class InvariantDoubleModelBinder : IModelBinder
    {
        private readonly SimpleTypeModelBinder _baseBinder;

        public InvariantDoubleModelBinder(Type modelType)
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

                if (double.TryParse(valueAsString.Replace(" ", "").Replace(".", ","), out var result))
                {
                    bindingContext.Result = ModelBindingResult.Success(result);
                    return Task.CompletedTask;
                }
                
            }
            return _baseBinder.BindModelAsync(bindingContext);
        }

    }
}