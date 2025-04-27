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

            try
            {
                if (!int.TryParse(id, out int customerId))
                {
                    _logger.LogWarning("Invalid customer ID: {Id}", id);
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
                        command.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = customerId });

                        try
                        {
                            await connection.OpenAsync();
                            _logger.LogInformation("Database connection opened successfully.");

                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    customer = new CustomerDetailDto
                                    {
                                        CustomerId = (int)reader["CustomerId"],
                                        FirstName = reader["FirstName"]?.ToString() ?? string.Empty,
                                        LastName = reader["LastName"]?.ToString() ?? string.Empty,
                                        Email = reader["Email"]?.ToString() ?? string.Empty,
                                        CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                                    };
                                }
                            }
                        }
                        catch (SqlException sqlEx)
                        {
                            _logger.LogError(sqlEx, "A database error occurred while executing the query for Customer ID: {CustomerId}.", customerId);
                            throw;
                        }
                    }
                }

                if (customer == null)
                {
                    _logger.LogWarning("Customer with ID {CustomerId} not found.", customerId);
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    return notFoundResponse;
                }

                var responseOk = req.CreateResponse(HttpStatusCode.OK);
                responseOk.Headers.Add("Content-Type", "application/json");
                string resultJson = JsonConvert.SerializeObject(customer);
                await responseOk.WriteStringAsync(resultJson);
                return responseOk;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request for Customer ID: {Id}.", id);
                throw; // Re-throw the exception to let the runtime handle it.
            }
        }
    }
}
