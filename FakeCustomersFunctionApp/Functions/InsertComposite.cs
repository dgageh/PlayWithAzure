using System.Data;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FakeCustomersFunctionApp.Models;

namespace FakeCustomersFunctionApp
{
    public class DataPopulationFunctions
    {
        private readonly ILogger _logger;

        public DataPopulationFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DataPopulationFunctions>();
        }

        [Function("InsertCompositeCustomer")]
        public async Task<HttpResponseData> InsertCompositeCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers")] HttpRequestData req)
        {
            _logger.LogInformation("InsertCompositeCustomer function triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var customerInput = JsonConvert.DeserializeObject<CustomerCompositeInputDto>(requestBody);
            if (customerInput == null ||
                string.IsNullOrWhiteSpace(customerInput.FirstName) ||
                string.IsNullOrWhiteSpace(customerInput.Email))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid customer data.");
                return badResponse;
            }

            // These lists will accumulate the newly inserted IDs.
            int newCustomerId;
            List<int> addressIds = new();
            List<int> phoneIds = new();
            List<int> orderIds = new();

            string? connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("The connection string is not configured in the environment variables.");
            }

            // Open a connection that will be reused across all insert operations.
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                newCustomerId = await InsertCustomerAsync(connection, customerInput);

                if (customerInput.Addresses != null)
                {
                    addressIds = await InsertAddressesAsync(connection, newCustomerId, customerInput.Addresses);
                }

                if (customerInput.Phones != null)
                {
                    phoneIds = await InsertPhonesAsync(connection, newCustomerId, customerInput.Phones);
                }

                if (customerInput.Orders != null)
                {
                    orderIds = await InsertOrdersAsync(connection, newCustomerId, customerInput.Orders);
                }
            }

            var responseDto = new CustomerInsertCompositeResponseDto
            {
                CustomerId = newCustomerId,
                AddressIds = addressIds,
                PhoneIds = phoneIds,
                OrderIds = orderIds
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(responseDto));
            return response;
        }

        private async Task<int> InsertCustomerAsync(SqlConnection connection, CustomerCompositeInputDto input)
        {
            using (var cmd = new SqlCommand("dbo.InsertCustomer", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.NVarChar, 100) { Value = input.FirstName });
                cmd.Parameters.Add(new SqlParameter("@LastName", SqlDbType.NVarChar, 100)
                {
                    Value = string.IsNullOrWhiteSpace(input.LastName) ? DBNull.Value : (object)input.LastName
                });
                cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 150) { Value = input.Email });
                // Assuming that the stored proc now returns an int.
                var outCustomerId = new SqlParameter("@NewCustomerId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outCustomerId);
                await cmd.ExecuteNonQueryAsync();
                return (int)outCustomerId.Value;
            }
        }

        private async Task<List<int>> InsertAddressesAsync(SqlConnection connection, int customerId, List<AddressInputDto> addresses)
        {
            var result = new List<int>();
            foreach (var addr in addresses)
            {
                using (var cmd = new SqlCommand("dbo.AddCustomerAddress", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    // Note: Ensure that the stored procedure accepts an int for CustomerId now.
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = customerId });
                    cmd.Parameters.Add(new SqlParameter("@StreetAddress", SqlDbType.NVarChar, 200) { Value = addr.StreetAddress });
                    cmd.Parameters.Add(new SqlParameter("@ZipCode", SqlDbType.NVarChar, 10) { Value = addr.ZipCode });
                    cmd.Parameters.Add(new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = addr.City });
                    cmd.Parameters.Add(new SqlParameter("@State", SqlDbType.Char, 2) { Value = addr.State });
                    var outAddressId = new SqlParameter("@NewAddressId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outAddressId);
                    await cmd.ExecuteNonQueryAsync();
                    result.Add((int)outAddressId.Value);
                }
            }
            return result;
        }

        private async Task<List<int>> InsertPhonesAsync(SqlConnection connection, int customerId, List<PhoneInputDto> phones)
        {
            var result = new List<int>();
            foreach (var phone in phones)
            {
                using (var cmd = new SqlCommand("dbo.AddCustomerPhone", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = customerId });
                    cmd.Parameters.Add(new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = phone.PhoneNumber });
                    cmd.Parameters.Add(new SqlParameter("@PhoneTypeName", SqlDbType.NVarChar, 50) { Value = phone.PhoneTypeName });
                    var outPhoneId = new SqlParameter("@NewPhoneId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outPhoneId);
                    await cmd.ExecuteNonQueryAsync();
                    result.Add((int)outPhoneId.Value);
                }
            }
            return result;
        }

        private async Task<List<int>> InsertOrdersAsync(SqlConnection connection, int customerId, List<OrderInputDto> orders)
        {
            var orderIds = new List<int>();
            foreach (var order in orders)
            {
                int newOrderId;
                using (var cmd = new SqlCommand("dbo.AddCustomerOrder", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = customerId });
                    var orderDateValue = order.OrderDate.HasValue ? (object)order.OrderDate.Value : DBNull.Value;
                    cmd.Parameters.Add(new SqlParameter("@OrderDate", SqlDbType.DateTime) { Value = orderDateValue });
                    var outOrderId = new SqlParameter("@NewOrderId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outOrderId);
                    await cmd.ExecuteNonQueryAsync();
                    newOrderId = (int)outOrderId.Value;
                    orderIds.Add(newOrderId);
                }

                // Insert Order Items for this order (if any).
                if (order.OrderItems != null)
                {
                    await InsertOrderItemsAsync(connection, newOrderId, order.OrderItems);
                }
            }
            return orderIds;
        }

        private async Task<List<int>> InsertOrderItemsAsync(SqlConnection connection, int orderId, List<OrderItemInputDto> items)
        {
            var orderItemIds = new List<int>();
            foreach (var item in items)
            {
                using (var cmd = new SqlCommand("dbo.AddOrderItem", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@OrderId", SqlDbType.Int) { Value = orderId });
                    cmd.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = item.ProductId });
                    cmd.Parameters.Add(new SqlParameter("@Quantity", SqlDbType.Int) { Value = item.Quantity });
                    cmd.Parameters.Add(new SqlParameter("@UnitPrice", SqlDbType.Decimal)
                    {
                        Value = item.UnitPrice,
                        Precision = 10,
                        Scale = 2
                    });
                    var outOrderItemId = new SqlParameter("@NewOrderItemId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outOrderItemId);
                    await cmd.ExecuteNonQueryAsync();
                    orderItemIds.Add(Convert.ToInt32(outOrderItemId.Value));
                }
            }
            return orderItemIds;
        }
    
        [Function("InsertProduct")]
        public async Task<HttpResponseData> InsertProduct(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequestData req)
        {
            _logger.LogInformation("InsertProduct function triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var productInput = JsonConvert.DeserializeObject<ProductInputDto>(requestBody);
            if (productInput == null || string.IsNullOrWhiteSpace(productInput.ProductName))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid product data.");
                return badResponse;
            }

            int newProductId;
            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new SqlCommand("dbo.InsertProduct", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@ProductName", SqlDbType.NVarChar, 150) { Value = productInput.ProductName });
                    cmd.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, -1) { Value = (object)productInput.Description ?? DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal)
                    {
                        Value = productInput.Price,
                        Precision = 10,
                        Scale = 2
                    });
                    cmd.Parameters.Add(new SqlParameter("@CategoryName", SqlDbType.NVarChar, 100) { Value = productInput.CategoryName });
                    var outProductId = new SqlParameter("@NewProductId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outProductId);
                    await cmd.ExecuteNonQueryAsync();
                    newProductId = Convert.ToInt32(outProductId.Value);
                }
            }

            var productResponse = new ProductInsertResponseDto { ProductId = newProductId };
            var responseSuccess = req.CreateResponse(HttpStatusCode.OK);
            responseSuccess.Headers.Add("Content-Type", "application/json");
            await responseSuccess.WriteStringAsync(JsonConvert.SerializeObject(productResponse));
            return responseSuccess;
        }
    }


}