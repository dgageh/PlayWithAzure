using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Azure.Data.Tables;

namespace TableStorageFunctionApp
{
    public class TableOperations
    {
        private readonly ILogger<TableOperations> _logger;

        public TableOperations(ILogger<TableOperations> logger)
        {
            _logger = logger;
        }

        [Function("GetAllNames")]
        public static async Task<HttpResponseData> GetAllNames(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "names")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("GetAllNames");
            logger.LogInformation("Fetching all names from Table Storage.");

            var tableClient = new TableClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "SortedNames");
            tableClient.CreateIfNotExists();

            var names = tableClient.Query<NameEntity>().ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(names);
            return response;
        }

        [Function("InsertName")]
        public static async Task<HttpResponseData> InsertName(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "names")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("InsertName");
            logger.LogInformation("Adding a new name to Table Storage.");

            var tableClient = new TableClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "SortedNames");
            tableClient.CreateIfNotExists();

            var requestBody = await req.ReadFromJsonAsync<NameEntity>();
            if (requestBody == null || string.IsNullOrWhiteSpace(requestBody.Name))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid input. Name cannot be empty.");
                return errorResponse;
            }

            requestBody.RowKey = Guid.NewGuid().ToString();  // Assign a unique RowKey
            requestBody.PartitionKey = "SortedNames";

            await tableClient.AddEntityAsync(requestBody);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteStringAsync($"Name '{requestBody.Name}' added successfully.");
            return response;
        }

        [Function("FindName")]
        public static async Task<HttpResponseData> FindName(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "names/{rowKey}")] HttpRequestData req,
            FunctionContext context, string rowKey)
        {
            var logger = context.GetLogger("FindName");
            logger.LogInformation($"Looking for name with RowKey = {rowKey}");

            var tableClient = new TableClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "SortedNames");

            tableClient.CreateIfNotExists();

            var entity = await tableClient.GetEntityAsync<NameEntity>("SortedNames", rowKey);

            var response = req.CreateResponse(entity != null ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            await response.WriteAsJsonAsync(entity);
            return response;
        }

        [Function("CountNames")]
        public static async Task<HttpResponseData> CountNames(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "names/count")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("CountNames");
            logger.LogInformation("Counting all names in Table Storage.");

            var tableClient = new TableClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "SortedNames");

            tableClient.CreateIfNotExists();

            var count = tableClient.Query<NameEntity>().Count();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Total names in table: {count}");
            return response;
        }
    }

    public class NameEntity : ITableEntity
    {
        public string? PartitionKey { get; set; } = "SortedNames";
        public string? RowKey { get; set; }  // Unique name identifier
        public required string Name { get; set; }
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
