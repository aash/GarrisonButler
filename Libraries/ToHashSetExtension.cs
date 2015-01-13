using System.Collections.Generic;

namespace GarrisonButler.Libraries
{
    public static class ToHashSetExtension
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }
    }
}