using Azure.Data.Tables;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
namespace DurableMergeSortApp
{
    public static class StoreSortedNames
    {
        [Function("StoreSortedNames")]
        public static async Task Run(
            [ActivityTrigger] List<string> sortedNames, FunctionContext context)
        {
            var log = context.GetLogger("StoreSortedNames");
            log.LogInformation($"Storing {sortedNames.Count} sorted names in Table Storage...");

            // Retrieve the connection string from an environment variable
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            if (string.IsNullOrEmpty(connectionString))
            {
                log.LogError("Table Storage connection string is not set in environment variables.");
                throw new InvalidOperationException("Table Storage connection string is missing.");
            }

            var tableServiceClient = new TableServiceClient(connectionString);
            var tableClient = tableServiceClient.GetTableClient("SortedNames");

            await tableClient.CreateIfNotExistsAsync();

            foreach (var name in sortedNames)
            {
                var entity = new NameEntity { PartitionKey = "SortedNames", RowKey = Guid.NewGuid().ToString(), Name = name };
                await tableClient.AddEntityAsync(entity);
            }

            log.LogInformation("Sorted names successfully stored.");
        }

        public class NameEntity : ITableEntity
        {
            public required string PartitionKey { get; set; }
            public required string RowKey { get; set; }
            public required string Name { get; set; }
            public ETag ETag { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
        }
    }
}
