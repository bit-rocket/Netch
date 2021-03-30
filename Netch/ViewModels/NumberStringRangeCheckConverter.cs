using System;
using ReactiveUI;

namespace Netch.ViewModels
{
    public class NumberStringRangeCheckConverter
    {
        public class NumberToStringConverter<TNum> : IBindingTypeConverter where TNum : notnull
        {
            public int GetAffinityForObjects(Type fromType, Type toType)
            {
                if (fromType is TNum && toType == typeof(string))
                    return 100;

                return 0;
            }

            public bool TryConvert(object? from, Type toType, object? conversionHint, out object? result)
            {
                throw new NotImplementedException();
            }
        }
    }
}