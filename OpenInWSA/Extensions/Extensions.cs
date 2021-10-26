using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace OpenInWSA.Extensions
{
    public static class Extensions
    {
        public static TSource ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int? index)
        {
            if (!index.HasValue) return default;

            return Enumerable.ElementAtOrDefault(source, index.Value);
        }

        public static ManagementBaseObject First(this ManagementObjectCollection source)
        {
            var results = source.GetEnumerator();
            results.MoveNext();
            return results.Current;
        }
    }
}