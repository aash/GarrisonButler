using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GarrisonButler.Libraries
{
    public static class IsNullOrEmptyExtension
    {
        public static bool IsNullOrEmpty(this IEnumerable source)
        {
            if (source == null) return true;
            return !source.Cast<object>().Any();
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null) return true;
            return !source.Any();
        }

        public static IEnumerable GetEmptyIfNull(this IEnumerable source)
        {
            return source ?? Enumerable.Empty<object>();
        }

        public static IEnumerable<T> GetEmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }
    }
}