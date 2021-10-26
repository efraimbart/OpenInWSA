using System.Collections.Generic;
using System.Linq;

namespace OpenInWSA.Extensions
{
    public static class Extensions
    {
        public static TSource ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int? index)
        {
            if (!index.HasValue) return default;

            return Enumerable.ElementAtOrDefault(source, index.Value);
        }
    }
}