using System;
using System.Collections.Generic;
using System.Linq;

namespace RavenVsMongo.Utils
{
    public static class ChunkUtils
    {
        public static IEnumerable<int> GetChunks(int generateCount, int bulkSize)
        {
            var repeatCount = generateCount / bulkSize;
            var chunkSizes = Enumerable.Repeat(bulkSize, repeatCount);
            var last = generateCount - repeatCount * bulkSize;
            if (last > 0)
                chunkSizes = chunkSizes.Concat(new[] { last });

            return chunkSizes;
        }

        public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> list, int count = Int32.MaxValue)
        {
            var random = new Random(Environment.TickCount);
            return list
                .Select(i => new {value = i, n = random.Next()})
                .OrderBy(i => i.n)
                .Select(i => i.value)
                .Take(count);
        }
    }
}