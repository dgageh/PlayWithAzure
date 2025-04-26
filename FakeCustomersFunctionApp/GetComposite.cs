using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

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

            // Instantiate our composite DTO.
            CustomerFetchedDto customer = null;
            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // --- 1. Retrieve Customer Basic Information ---
                using (var cmd = new SqlCommand(
                    "SELECT CustomerId, FirstName, LastName, Email, CreatedDate FROM dbo.Customer WHERE CustomerId = @CustomerId",
                    connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = customerId });
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            customer = new CustomerFetchedDto
                            {
                                CustomerId = (Guid)reader["CustomerId"],
                                FirstName = reader["FirstName"].ToString(),
                                LastName = reader["LastName"].ToString(),
                                Email = reader["Email"].ToString(),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                Addresses = new List<AddressDto>(),
                                Phones = new List<PhoneDto>(),
                                Orders = new List<OrderDto>()
                            };
                        }
                    }
                }

                if (customer == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync("Customer not found.");
                    return notFoundResponse;
                }

                // --- 2. Retrieve Addresses ---
                using (var cmd = new SqlCommand(
                    "SELECT AddressId, StreetAddress, ZipCode, City, State FROM dbo.Address WHERE CustomerId = @CustomerId",
                    connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = customerId });
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var addr = new AddressDto
                            {
                                AddressId = (Guid)reader["AddressId"],
                                StreetAddress = reader["StreetAddress"].ToString(),
                                ZipCode = reader["ZipCode"].ToString(),
                                City = reader["City"].ToString(),
                                State = reader["State"].ToString()
                            };
                            customer.Addresses.Add(addr);
                        }
                    }
                }

                // --- 3. Retrieve Phones ---
                using (var cmd = new SqlCommand(
                    "SELECT cp.PhoneId, cp.PhoneNumber, pt.PhoneTypeName " +
                    "FROM dbo.CustomerPhone cp " +
                    "JOIN dbo.PhoneType pt ON cp.PhoneTypeId = pt.PhoneTypeId " +
                    "WHERE cp.CustomerId = @CustomerId",
                    connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = customerId });
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var phone = new PhoneDto
                            {
                                PhoneId = (Guid)reader["PhoneId"],
                                PhoneNumber = reader["PhoneNumber"].ToString(),
                                PhoneTypeName = reader["PhoneTypeName"].ToString()
                            };
                            customer.Phones.Add(phone);
                        }
                    }
                }

                // --- 4. Retrieve Orders ---
                // We get orders first, then for each order get the order items.
                var orders = new List<OrderDto>();
                using (var cmd = new SqlCommand(
                    "SELECT OrderId, OrderDate FROM dbo.[Order] WHERE CustomerId = @CustomerId",
                    connection))
                {
                    cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = customerId });
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var orderDto = new OrderDto
                            {
                                OrderId = (Guid)reader["OrderId"],
                                OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                                OrderItems = new List<OrderItemDto>()
                            };
                            orders.Add(orderDto);
                        }
                    }
                }

                // --- 5. Retrieve Order Items for Each Order ---
                foreach (var order in orders)
                {
                    var orderItems = new List<OrderItemDto>();
                    using (var cmd = new SqlCommand(
                        "SELECT OrderItemId, ProductId, Quantity, UnitPrice FROM dbo.OrderItem WHERE OrderId = @OrderId",
                        connection))
                    {
                        cmd.Parameters.Add(new SqlParameter("@OrderId", SqlDbType.UniqueIdentifier) { Value = order.OrderId });
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new OrderItemDto
                                {
                                    OrderItemId = (int)reader["OrderItemId"],
                                    ProductId = (int)reader["ProductId"],
                                    Quantity = (int)reader["Quantity"],
                                    UnitPrice = (decimal)reader["UnitPrice"]
                                };
                                orderItems.Add(item);
                            }
                        }
                    }
                    order.OrderItems = orderItems;
                }
                customer.Orders = orders;
            } // End using connection

            // Build the final response.
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            string jsonResponse = JsonConvert.SerializeObject(customer);
            await response.WriteStringAsync(jsonResponse);
            return response;
        }
    }

    // --- DTO Definitions for Composite Retrieval ---

    // Composite DTO for a customer with related data.
    public class CustomerFetchedDto
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<AddressDto> Addresses { get; set; }
        public List<PhoneDto> Phones { get; set; }
        public List<OrderDto> Orders { get; set; }
    }

    public class AddressDto
    {
        public int AddressId { get; set; }
        public string StreetAddress { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }

    public class PhoneDto
    {
        public int PhoneId { get; set; }
        public string PhoneNumber { get; set; }
        public string PhoneTypeName { get; set; }
    }

    public class OrderDto
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
    }

    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
