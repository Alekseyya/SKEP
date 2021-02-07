using System;

namespace Core.Extensions
{
    public static class DynamicExtention
    {
        public static DateTime ConvertToType(dynamic source, Type dest)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (dest == null) throw new ArgumentNullException(nameof(dest));
            return Convert.ChangeType(source, dest);
        }
    }
}