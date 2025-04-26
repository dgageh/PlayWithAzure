using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Data;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Microsoft.Data.SqlClient;
using FakeCustomersFunctionApp.Models;


namespace FakeCustomersFunctionApp
{
    public class CustomerFunctions
    {
        private readonly ILogger _logger;

        public CustomerFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CustomerFunctions>();
        }


        [Function("GetCustomer")]
        public async Task<HttpResponseData> GetCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("GetCustomer function triggered.");

            if (!Guid.TryParse(id, out Guid customerId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid customer id.");
                return badResponse;
            }

            CustomerDetailDto? customer = null;
            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT CustomerId, FirstName, LastName, Email, CreatedDate FROM dbo.Customer WHERE CustomerId = @CustomerId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = customerId });
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            customer = new CustomerDetailDto
                            {
                                CustomerId = (Guid)reader["CustomerId"],
                                FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                                LastName = reader["LastName"]?.ToString() ?? string.Empty,
                                Email = reader["Email"]?.ToString() ?? string.Empty,
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                            };
                        }
                    }
                }
            }

            if (customer == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                return notFoundResponse;
            }

            var responseOk = req.CreateResponse(HttpStatusCode.OK);
            responseOk.Headers.Add("Content-Type", "application/json");
            string resultJson = JsonConvert.SerializeObject(customer);
            await responseOk.WriteStringAsync(resultJson);
            return responseOk;
        }
    }
}
