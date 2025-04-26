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
    public class DataRetrievalFunctions
    {
        private readonly ILogger _logger;

        public DataRetrievalFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DataRetrievalFunctions>();
        }

        [Function("GetCompositeCustomer")]
        public async Task<HttpResponseData> GetCompositeCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/composite/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("GetCompositeCustomer function triggered.");

            // Validate the ID.
            if (!Guid.TryParse(id, out Guid customerId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid customer id.");
                return badResponse;
            }

            CustomerResponseDto? customer;
            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Get the basic customer information.
                customer = await GetCustomerBasicAsync(connection, customerId);

                // If no customer was found, return immediately.
                if (customer == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync("Customer not found.");
                    return notFoundResponse;
                }

                // Populate the associated collections.
                customer.Addresses = await GetAddressesAsync(connection, customerId);
                customer.Phones = await GetPhonesAsync(connection, customerId);
                customer.Orders = await GetOrdersAsync(connection, customerId);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            string jsonResponse = JsonConvert.SerializeObject(customer);
            await response.WriteStringAsync(jsonResponse);
            return response;
        }

        private async Task<CustomerResponseDto?> GetCustomerBasicAsync(SqlConnection connection, Guid customerId)
        {
            using (var cmd = new SqlCommand(
                "SELECT CustomerId, FirstName, LastName, Email, CreatedDate FROM dbo.Customer WHERE CustomerId = @CustomerId",
                connection))
            {
                cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = customerId });
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new CustomerResponseDto
                        {
                            CustomerId = (int)reader["CustomerId"],
                            FirstName = reader["FirstName"].ToString(),
                            LastName = reader["LastName"].ToString(),
                            Email = reader["Email"].ToString(),
                            CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                            Addresses = new List<AddressResponseDto>(),
                            Phones = new List<PhoneResponseDto>(),
                            Orders = new List<OrderDto>()
                        };
                    }
                }
            }
            return null;
        }

        private async Task<List<AddressResponseDto>> GetAddressesAsync(SqlConnection connection, Guid customerId)
        {
            var addresses = new List<AddressResponseDto>();
            using (var cmd = new SqlCommand(
                "SELECT AddressId, StreetAddress, ZipCode, City, State FROM dbo.Address WHERE CustomerId = @CustomerId",
                connection))
            {
                cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = customerId });
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        addresses.Add(new AddressResponseDto
                        {
                            AddressId = (int)reader["AddressId"],
                            StreetAddress = reader["StreetAddress"].ToString(),
                            ZipCode = reader["ZipCode"].ToString(),
                            City = reader["City"].ToString(),
                            State = reader["State"].ToString()
                        });
                    }
                }
            }
            return addresses;
        }

        private async Task<List<PhoneResponseDto>> GetPhonesAsync(SqlConnection connection, Guid customerId)
        {
            var phones = new List<PhoneResponseDto>();
            using (var cmd = new SqlCommand(
                "SELECT cp.PhoneId, cp.PhoneNumber, pt.PhoneTypeName " +
                "FROM dbo.CustomerPhone cp " +
                "JOIN dbo.PhoneType pt ON cp.PhoneTypeId = pt.PhoneTypeId " +
                "WHERE cp.CustomerId = @CustomerId", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = customerId });
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        phones.Add(new PhoneResponseDto
                        {
                            PhoneId = (int)reader["PhoneId"],
                            PhoneNumber = reader["PhoneNumber"].ToString(),
                            PhoneType = reader["PhoneTypeName"].ToString()
                        });
                    }
                }
            }
            return phones;
        }

        private async Task<List<OrderDto>> GetOrdersAsync(SqlConnection connection, Guid customerId)
        {
            var orders = new List<OrderDto>();
            using (var cmd = new SqlCommand(
                "SELECT OrderId, OrderDate FROM dbo.[Order] WHERE CustomerId = @CustomerId", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = customerId });
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        orders.Add(new OrderDto
                        {
                            OrderId = (int)reader["OrderId"],
                            OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                            OrderItems = new List<OrderItemDto>()
                        });
                    }
                }
            }

            // For each order, retrieve its order items.
            foreach (var order in orders)
            {
                order.OrderItems = await GetOrderItemsAsync(connection, order.OrderId);
            }
            return orders;
        }

        private async Task<List<OrderItemDto>> GetOrderItemsAsync(SqlConnection connection, int orderId)
        {
            var orderItems = new List<OrderItemDto>();
            using (var cmd = new SqlCommand(
                "SELECT OrderItemId, ProductId, Quantity, UnitPrice FROM dbo.OrderItem WHERE OrderId = @OrderId", connection))
            {
                cmd.Parameters.Add(new SqlParameter("@OrderId", SqlDbType.UniqueIdentifier) { Value = orderId });
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        orderItems.Add(new OrderItemDto
                        {
                            OrderItemId = (int)reader["OrderItemId"],
                            ProductId = (int)reader["ProductId"],
                            Quantity = (int)reader["Quantity"],
                            UnitPrice = (decimal)reader["UnitPrice"]
                        });
                    }
                }
            }
            return orderItems;
        }
    }

}