using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DurableMergeSortApp
{



    public static class PerformMergeSort
    {
        [Function("PerformMergeSort")]
        public static List<string> Run(
            [ActivityTrigger] List<string> names, FunctionContext context)
        {
            var log = context.GetLogger("PerformMergeSort");
            log.LogInformation($"Starting merge sort on {names.Count} names...");

            var sortedNames = MergeSort(names);

            log.LogInformation("Merge sort completed.");
            return sortedNames;
        }

        private static List<string> MergeSort(List<string> names)
        {
            if (names.Count <= 1) return names;

            int mid = names.Count / 2;
            var left = MergeSort(names.Take(mid).ToList());
            var right = MergeSort(names.Skip(mid).ToList());

            return Merge(left, right);
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
