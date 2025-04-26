using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

            // Accumulate IDs as we process.
            int newCustomerId;
            var addressIds = new List<int>();
            var phoneIds = new List<int>();
            var orderResponses = new List<OrderResponseDto>();

            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            // Open a connection that will be reused.
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // --- Insert Customer ---
                using (var cmd = new SqlCommand("dbo.InsertCustomer", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.NVarChar, 100) { Value = customerInput.FirstName });
                    cmd.Parameters.Add(new SqlParameter("@LastName", SqlDbType.NVarChar, 100) { Value = string.IsNullOrWhiteSpace(customerInput.LastName) ? DBNull.Value : (object)customerInput.LastName });
                    cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 150) { Value = customerInput.Email });
                    var outCustomerId = new SqlParameter("@NewCustomerId", SqlDbType.UniqueIdentifier)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outCustomerId);
                    await cmd.ExecuteNonQueryAsync();
                    newCustomerId = (int)outCustomerId.Value;
                }

                // --- Insert Addresses if provided ---
                if (customerInput.Addresses != null)
                {
                    foreach (var addr in customerInput.Addresses)
                    {
                        using (var cmd = new SqlCommand("dbo.AddCustomerAddress", connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = newCustomerId });
                            cmd.Parameters.Add(new SqlParameter("@StreetAddress", SqlDbType.NVarChar, 200) { Value = addr.StreetAddress });
                            cmd.Parameters.Add(new SqlParameter("@ZipCode", SqlDbType.NVarChar, 10) { Value = addr.ZipCode });
                            cmd.Parameters.Add(new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = addr.City });
                            cmd.Parameters.Add(new SqlParameter("@State", SqlDbType.Char, 2) { Value = addr.State });
                            var outAddressId = new SqlParameter("@NewAddressId", SqlDbType.UniqueIdentifier)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(outAddressId);
                            await cmd.ExecuteNonQueryAsync();
                            addressIds.Add((int)outAddressId.Value);
                        }
                    }
                }

                // --- Insert Phones if provided ---
                if (customerInput.Phones != null)
                {
                    foreach (var phone in customerInput.Phones)
                    {
                        using (var cmd = new SqlCommand("dbo.AddCustomerPhone", connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = newCustomerId });
                            cmd.Parameters.Add(new SqlParameter("@PhoneNumber", SqlDbType.NVarChar, 20) { Value = phone.PhoneNumber });
                            cmd.Parameters.Add(new SqlParameter("@PhoneTypeName", SqlDbType.NVarChar, 50) { Value = phone.PhoneTypeName });
                            var outPhoneId = new SqlParameter("@NewPhoneId", SqlDbType.UniqueIdentifier)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(outPhoneId);
                            await cmd.ExecuteNonQueryAsync();
                            phoneIds.Add((int)outPhoneId.Value);
                        }
                    }
                }

                // --- Insert Orders if provided ---
                if (customerInput.Orders != null)
                {
                    foreach (var order in customerInput.Orders)
                    {
                        int newOrderId;
                        using (var cmd = new SqlCommand("dbo.AddCustomerOrder", connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) { Value = newCustomerId });
                            var orderDateValue = order.OrderDate.HasValue ? (object)order.OrderDate.Value : DBNull.Value;
                            cmd.Parameters.Add(new SqlParameter("@OrderDate", SqlDbType.DateTime) { Value = orderDateValue });
                            var outOrderId = new SqlParameter("@NewOrderId", SqlDbType.UniqueIdentifier)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(outOrderId);
                            await cmd.ExecuteNonQueryAsync();
                            newOrderId = (int)outOrderId.Value;
                        }

                        var orderItemIds = new List<int>();

                        // --- Insert Order Items for this order, if any ---
                        if (order.OrderItems != null)
                        {
                            foreach (var item in order.OrderItems)
                            {
                                using (var cmd = new SqlCommand("dbo.AddOrderItem", connection))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.Add(new SqlParameter("@OrderId", SqlDbType.UniqueIdentifier) { Value = newOrderId });
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
                        }
                        orderResponses.Add(new OrderResponseDto { OrderId = newOrderId, OrderItemIds = orderItemIds });
                    }
                }
            } // End using connection

            // Build the composite response DTO.
            var responseDto = new CustomerCompositeResponseDto
            {
                CustomerId = newCustomerId,
                AddressIds = addressIds,
                PhoneIds = phoneIds,
                Orders = orderResponses
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonConvert.SerializeObject(responseDto));
            return response;
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

            var productResponse = new ProductResponseDto { ProductId = newProductId };
            var responseSuccess = req.CreateResponse(HttpStatusCode.OK);
            responseSuccess.Headers.Add("Content-Type", "application/json");
            await responseSuccess.WriteStringAsync(JsonConvert.SerializeObject(productResponse));
            return responseSuccess;
        }
    }

    // --- DTO Definitions ---

    // Customer Composite Input DTO contains basic customer info plus optional arrays.
    public class CustomerCompositeInputDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public List<AddressInputDto> Addresses { get; set; }
        public List<PhoneInputDto> Phones { get; set; }
        public List<OrderInputDto> Orders { get; set; }
    }

    public class AddressInputDto
    {
        public string StreetAddress { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }

    public class PhoneInputDto
    {
        public string PhoneNumber { get; set; }
        public string PhoneTypeName { get; set; }
    }

    public class OrderInputDto
    {
        public DateTime? OrderDate { get; set; }
        public List<OrderItemInputDto> OrderItems { get; set; }
    }

    public class OrderItemInputDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // Composite response DTO for customer insertion.
    public class CustomerCompositeResponseDto
    {
        public int CustomerId { get; set; }
        public List<int> AddressIds { get; set; }
        public List<int> PhoneIds { get; set; }
        public List<OrderResponseDto> Orders { get; set; }
    }

    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public List<int> OrderItemIds { get; set; }
    }

    // Input DTO for product insertion.
    public class ProductInputDto
    {
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; }
    }

    // Response DTO for product insertion.
    public class ProductResponseDto
    {
        public int ProductId { get; set; }
    }
}