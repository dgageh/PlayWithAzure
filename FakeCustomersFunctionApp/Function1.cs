using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Data;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Microsoft.Data.SqlClient;


namespace FakeCustomersFunctionApp
{
    public class FakeCustomersFunctions
    {
        private readonly ILogger<FakeCustomersFunctions> _logger;

        public FakeCustomersFunctions(ILogger<FakeCustomersFunctions> logger)
        {
            _logger = logger;
        }

        public class CustomerFunctions
        {
            private readonly ILogger _logger;

            public CustomerFunctions(ILoggerFactory loggerFactory)
            {
                _logger = loggerFactory.CreateLogger<CustomerFunctions>();
            }

            [Function("InsertCustomer")]
            public async Task<HttpResponseData> InsertCustomer(
                [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers/insert")] HttpRequestData req)
            {
                _logger.LogInformation("InsertCustomer function triggered.");

                // Read and deserialize the JSON body.
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var customerDto = JsonConvert.DeserializeObject<CustomerDto>(requestBody);
                if (customerDto == null ||
                    string.IsNullOrWhiteSpace(customerDto.FirstName) ||
                    string.IsNullOrWhiteSpace(customerDto.Email))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid customer data.");
                    return badResponse;
                }

                Guid newCustomerId;
                var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

                // Use raw ADO.NET to call the stored procedure.
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("dbo.InsertCustomer", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.NVarChar, 100) { Value = customerDto.FirstName });
                    command.Parameters.Add(new SqlParameter("@LastName", SqlDbType.NVarChar, 100) { Value = customerDto.LastName });
                    command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 150) { Value = customerDto.Email });

                    // Create the output parameter.
                    SqlParameter outputParam = new SqlParameter("@NewCustomerId", SqlDbType.UniqueIdentifier)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputParam);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    newCustomerId = (Guid)outputParam.Value;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                string jsonResponse = JsonConvert.SerializeObject(new { CustomerId = newCustomerId });
                await response.WriteStringAsync(jsonResponse);
                return response;
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

        // DTO for inserting a customer.
        public class CustomerDto
        {
            public required string FirstName { get; set; }
            public required string LastName { get; set; }
            public required string Email { get; set; }
        }

        // DTO for returning customer details.
        public class CustomerDetailDto
        {
            public Guid CustomerId { get; set; }
            public required string FirstName { get; set; }
            public required string LastName { get; set; }
            public required string Email { get; set; }
            public DateTime CreatedDate { get; set; }
        }
    }
}
