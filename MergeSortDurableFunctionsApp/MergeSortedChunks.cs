
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace DurableMergeSortApp
{
    public static class MergeSortedChunks
    {
        [Function("MergeSortedChunks")]
        public static List<string> Run(
            [ActivityTrigger] List<List<string>> sortedChunks, FunctionContext context)
        {
            var log = context.GetLogger("PerformMergeSort");
            log.LogInformation($"Merging {sortedChunks.Count} sorted chunks...");

            var mergedList = MergeAllChunks(sortedChunks);

            log.LogInformation("Chunk merging completed.");
            return mergedList;
        }

        private static List<string> MergeAllChunks(List<List<string>> sortedChunks)
        {
            var sortedList = new List<string>();

            foreach (var chunk in sortedChunks)
            {
                sortedList = Merge(sortedList, chunk);
            }

            return sortedList;
        }

        private static List<string> Merge(List<string> left, List<string> right)
        {
            var result = new List<string>();
            int i = 0, j = 0;

            while (i < left.Count && j < right.Count)
            {
                if (string.Compare(left[i], right[j]) < 0)
                {
                    result.Add(left[i]);
                    i++;
                }
                else
                {
                    result.Add(right[j]);
                    j++;
                }
            }

            result.AddRange(left.Skip(i));
            result.AddRange(right.Skip(j));

            return result;
        }
    }
}
