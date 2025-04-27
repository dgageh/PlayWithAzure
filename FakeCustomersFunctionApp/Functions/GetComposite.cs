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

            try
            {
                if (!int.TryParse(id, out int customerId))
                {
                    _logger.LogWarning("Invalid customer ID: {Id}", id);
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid customer id.");
                    return badResponse;
                }

                CustomerResponseDto? customer;
                var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Database connection opened successfully.");

                    customer = await GetCustomerBasicAsync(connection, customerId);

                    if (customer == null)
                    {
                        _logger.LogWarning("Customer with ID {CustomerId} not found.", customerId);
                        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                        await notFoundResponse.WriteStringAsync("Customer not found.");
                        return notFoundResponse;
                    }

                    customer.Addresses = await GetAddressesAsync(connection, customerId);
                    customer.Phones = await GetPhonesAsync(connection, customerId);
                    customer.Orders = await GetOrdersAsync(connection, customerId);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                string jsonResponse = JsonConvert.SerializeObject(customer);
                await response.WriteStringAsync(jsonResponse);
                _logger.LogInformation("Customer data successfully retrieved and returned for ID {CustomerId}.", customerId);
                return response;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "A database error occurred while processing the request.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("A database error occurred. Please try again later.");
                return errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An unexpected error occurred. Please try again later.");
                return errorResponse;
            }
        }

        private async Task<CustomerResponseDto?> GetCustomerBasicAsync(SqlConnection connection, int customerId)
        {
            try
            {
                using (var cmd = new SqlCommand(
                    "SELECT CustomerId, FirstName, LastName, Email, CreatedDate FROM dbo.Customer WHERE CustomerId = @CustomerId",
                    connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = customerId });
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving basic customer information for ID {CustomerId}.", customerId);
                throw;
            }
            return null;
        }

        private async Task<List<AddressResponseDto>> GetAddressesAsync(SqlConnection connection, int customerId)
        {
            var addresses = new List<AddressResponseDto>();
            try
            {
                using (var cmd = new SqlCommand(
                    "SELECT AddressId, StreetAddress, Address.ZipCode, City, [State] FROM dbo.Address LEFT JOIN ZipCode on Address.ZipCode = ZipCode.ZipCode WHERE CustomerId = @CustomerId",
                    connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = customerId });
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving addresses for customer ID {CustomerId}.", customerId);
                throw;
            }
            return addresses;
        }

        private async Task<List<PhoneResponseDto>> GetPhonesAsync(SqlConnection connection, int customerId)
        {
            var phones = new List<PhoneResponseDto>();
            try
            {
                using (var cmd = new SqlCommand(
                    "SELECT cp.PhoneId, cp.PhoneNumber, pt.PhoneType " +
                    "FROM dbo.CustomerPhone cp " +
                    "LEFT JOIN dbo.PhoneType pt ON cp.PhoneTypeId = pt.PhoneTypeId " +
                    "WHERE cp.CustomerId = @CustomerId", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = customerId });
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            phones.Add(new PhoneResponseDto
                            {
                                PhoneId = (int)reader["PhoneId"],
                                PhoneNumber = reader["PhoneNumber"].ToString(),
                                PhoneType = reader["PhoneType"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving phone numbers for customer ID {CustomerId}.", customerId);
                throw;
            }
            return phones;
        }

        private async Task<List<OrderDto>> GetOrdersAsync(SqlConnection connection, int customerId)
        {
            var orders = new List<OrderDto>();
            try
            {
                using (var cmd = new SqlCommand(
                    "SELECT OrderId, OrderDate FROM dbo.[Order] WHERE CustomerId = @CustomerId", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = customerId });
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orders.Add(new OrderDto
                            {
                                OrderId = (int)reader["OrderId"],
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                OrderItems = new List<OrderItemResponseDto>()
                            });
                        }
                    }
                }

                foreach (var order in orders)
                {
                    order.OrderItems = await GetOrderItemsAsync(connection, order.OrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving orders for customer ID {CustomerId}.", customerId);
                throw;
            }
            return orders;
        }

        private async Task<List<OrderItemResponseDto>> GetOrderItemsAsync(SqlConnection connection, int orderId)
        {
            var orderItems = new List<OrderItemResponseDto>();
            try
            {
                using (var cmd = new SqlCommand(
                    "SELECT OrderItemId, OrderItem.ProductId, Quantity, UnitPrice, ProductName, CategoryName, Description FROM dbo.OrderItem LEFT JOIN dbo.Product ON OrderItem.ProductId = Product.ProductId LEFT JOIN dbo.ProductCategory on Product.CategoryId = ProductCategory.CategoryId  WHERE OrderId = @OrderId", connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@OrderId", SqlDbType.Int) { Value = orderId });
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orderItems.Add(new OrderItemResponseDto
                            {
                                OrderItemId = (int)reader["OrderItemId"],
                                ProductId = (int)reader["ProductId"],
                                ProductName = reader["ProductName"].ToString(),
                                CategoryName = reader["CategoryName"].ToString(),
                                Description = reader["Description"].ToString(),
                                Quantity = (int)reader["Quantity"],
                                UnitPrice = (decimal)reader["UnitPrice"]                                
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving order items for order ID {OrderId}.", orderId);
                throw;
            }
            return orderItems;
        }
    }

}