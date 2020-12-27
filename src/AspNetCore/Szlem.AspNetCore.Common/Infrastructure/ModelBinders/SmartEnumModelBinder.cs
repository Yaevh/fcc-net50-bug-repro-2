using Ardalis.GuardClauses;
using Ardalis.SmartEnum;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Szlem.AspNetCore.Common.Infrastructure.ModelBinders
{
    public class SmartEnumModelBinder<TSmartEnum> : CustomModelBinderBase<TSmartEnum>
        where TSmartEnum : SmartEnum<TSmartEnum>
    {
        private readonly Func<int, TSmartEnum> _fromValueMethod =
            (Func<int, TSmartEnum>) Delegate.CreateDelegate(
                typeof(Func<int, TSmartEnum>),
                typeof(TSmartEnum)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                    .Single(x => x.Name == nameof(SmartEnum<TSmartEnum>.FromValue) && x.GetParameters().Length == 1));

        protected override bool TryParse(ValueProviderResult value, out TSmartEnum result)
        {
            if (int.TryParse(value.FirstValue, out var intValue))
            {
                result = _fromValueMethod.Invoke(intValue);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }

    public class SmartEnumModelBinderProvider : IModelBinderProvider
    {
        private static readonly Type _smartEnumBaseType = typeof(SmartEnum<,>);

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            Guard.Against.Null(context, nameof(context));

            if (EnumerateBaseTypes(context.Metadata.ModelType)
                .Where(x => x.IsGenericType)
                .Any(x => x.GetGenericTypeDefinition() == _smartEnumBaseType))
            {
                var modelBinderType = typeof(SmartEnumModelBinder<>).MakeGenericType(context.Metadata.ModelType);
                return Activator.CreateInstance(modelBinderType) as IModelBinder;
            }
            else
            {
                return null;
            }
        }

        private IEnumerable<Type> EnumerateBaseTypes(Type type)
        {
            while(type.BaseType != null)
            {
                yield return type.BaseType;
                type = type.BaseType;
            }
        }
    }
}
