using System;

namespace Core.Extensions
{
    public static class BooleanExtention
    {
        public static bool NullableBoolToBool(this Nullable<Boolean> value) => value.HasValue ? value.Value : false;

        public static string BoolToYOrN(this Boolean value) => value ? "Y" : "N";
    }
}