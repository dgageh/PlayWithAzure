using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask; 

namespace DurableMergeSortApp
{
    public class MergeSortInput
    {
        public int Count { get; set; }
    }

    public static class MergeSortOrchestrator
    {
        [Function("MergeSortOrchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var logger = context.CreateReplaySafeLogger("MergeSortOrchestrator");
            logger.LogInformation("Starting MergeSortOrchestrator...");

            var input = context.GetInput<MergeSortInput>();
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input), "Input cannot be null.");
            }
            int nameCount = input.Count;
            // Generate fake names
            List<string> names = await context.CallActivityAsync<List<string>>("GenerateFakeNames", nameCount);

            // Split names into chunks for parallel sorting
            int chunkSize = names.Count / 4; // Example: Split into 4 chunks
            var chunks = names.Select((name, index) => new { name, index })
                              .GroupBy(x => x.index / chunkSize)
                              .Select(g => g.Select(x => x.name).ToList())
                              .ToList();

            // Sort chunks in parallel
            var sortingTasks = new List<Task<List<string>>>();
            foreach (var chunk in chunks)
            {
                sortingTasks.Add(context.CallActivityAsync<List<string>>("PerformMergeSort", chunk));
            }

            var sortedChunks = await Task.WhenAll(sortingTasks);

            // Merge sorted chunks
            var finalSortedNames = await context.CallActivityAsync<List<string>>("MergeSortedChunks", sortedChunks.ToList());

            // Store results
            await context.CallActivityAsync("StoreSortedNames", finalSortedNames);

            return finalSortedNames;
        }
    }
}
