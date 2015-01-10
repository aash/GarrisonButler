using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

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