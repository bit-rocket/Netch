using System;
using ReactiveUI;

namespace Netch.ViewModels
{
    public class BoolLocalAddressConverter
    {
        public class BoolToLocalAddressConverter : IBindingTypeConverter
        {
            public static BoolToLocalAddressConverter Instance = new();

            private BoolToLocalAddressConverter()
            {
            }

            public int GetAffinityForObjects(Type fromType, Type toType)
            {
                if (toType == typeof(string) && fromType == typeof(bool))
                    return 100;

                return 0;
            }

            public bool TryConvert(object? from, Type toType, object? conversionHint, out object? result)
            {
                result = (bool?) from ?? false ? "0.0.0.0" : "127.0.0.1";
                return true;
            }
        }

        public class LocalAddressToBoolConverter : IBindingTypeConverter
        {
            public static LocalAddressToBoolConverter Instance = new();

            private LocalAddressToBoolConverter()
            {
            }

            public int GetAffinityForObjects(Type fromType, Type toType)
            {
                if (toType == typeof(bool) && fromType == typeof(string))
                    return 100;

                return 0;
            }

            public bool TryConvert(object? from, Type toType, object? conversionHint, out object? result)
            {
                result = (string?) from == "127.0.0.1";
                return true;
            }
        }
    }
}