using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using Microsoft.DurableTask.Client;

namespace DurableMergeSortApp
{
    public static class StartMergeSortFunction
    {
        [Function("StartMergeSort")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient durableClient,
            FunctionContext context)
        {
            var logger = context.GetLogger("StartMergeSort");
            logger.LogInformation("Received merge sort request.");

            // Read the input from the request
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var inputData = JsonConvert.DeserializeObject<MergeSortRequest>(requestBody);

            if (inputData == null || inputData.NumberOfNames <= 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid input: Please provide a positive number.");
                return badResponse;
            }

            // Start the orchestration using the injected Durable Client
            string instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync("MergeSortOrchestrator", new { Count = inputData.NumberOfNames });

            logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            // Return instance ID for tracking
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Merge sort orchestration started. Instance ID: {instanceId}");
            return response;
        }
        private class MergeSortRequest
        {
            public int NumberOfNames { get; set; }
        }
    }
}
