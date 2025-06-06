﻿using System.Data;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FakeCustomersFunctionApp.Models;

namespace FakeCustomersFunctionApp.Functions
{
    namespace FakeCustomersFunctionApp
    {
        public class ProductFunctions
        {
            private readonly ILogger _logger;

            public ProductFunctions(ILoggerFactory loggerFactory)
            {
                _logger = loggerFactory.CreateLogger<ProductFunctions>();
            }

            // GET: /api/products
            [Function("GetAllProducts")]
            public async Task<HttpResponseData> GetAllProducts(
                [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products")] HttpRequestData req)
            {
                _logger.LogInformation("GetAllProducts function triggered.");

                try
                {
                    string? connectionStringNullable = Environment.GetEnvironmentVariable("SqlConnectionString");
                    if (string.IsNullOrEmpty(connectionStringNullable))
                    {
                        throw new InvalidOperationException("The SQL connection string is not configured in the environment variables.");
                    }

                    string connectionString = connectionStringNullable;
                    var products = new List<ProductResponseDto>();

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        _logger.LogInformation("Database connection opened successfully for GetAllProducts.");

                        using (var cmd = new SqlCommand(
                            "SELECT ProductId, ProductName, Description, Price, CategoryName from dbo.Product LEFT JOIN dbo.ProductCategory on Product.CategoryId = ProductCategory.CategoryId",
                            connection))
                        {
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var product = new ProductResponseDto
                                    {
                                        ProductId = (int)reader["ProductId"],
                                        ProductName = reader["ProductName"].ToString() ?? string.Empty,
                                        Description = reader["Description"] as string,
                                        Price = (decimal)reader["Price"],
                                        CategoryName = reader["CategoryName"].ToString() ?? string.Empty
                                    };
                                    products.Add(product);
                                }
                            }
                        }
                    }

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");
                    string jsonResponse = JsonConvert.SerializeObject(products);
                    await response.WriteStringAsync(jsonResponse);
                    return response;
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "A database error occurred while retrieving all products.");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred in GetAllProducts.");
                    throw;
                }
            }

            // GET: /api/products/{id}
            [Function("GetProductById")]
            public async Task<HttpResponseData> GetProductById(
                [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{id:int}")] HttpRequestData req,
                int id)
            {
                _logger.LogInformation($"GetProductById function triggered for ProductId: {id}");

                try
                {
                    string? connectionStringNullable = Environment.GetEnvironmentVariable("SqlConnectionString");
                    if (string.IsNullOrEmpty(connectionStringNullable))
                    {
                        throw new InvalidOperationException("The SQL connection string is not configured in the environment variables.");
                    }
                    string connectionString = connectionStringNullable;

                    ProductResponseDto? product = null;

                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        _logger.LogInformation("Database connection opened successfully for GetProductById.");

                        using (var cmd = new SqlCommand(
                            "SELECT ProductId, ProductName, Description, Price, CategoryName from dbo.Product LEFT JOIN dbo.ProductCategory on Product.CategoryId = ProductCategory.CategoryId WHERE ProductId = @ProductId",
                            connection))
                        {
                            cmd.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = id });

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    product = new ProductResponseDto
                                    {
                                        ProductId = (int)reader["ProductId"],
                                        ProductName = reader["ProductName"].ToString() ?? string.Empty,
                                        Description = reader["Description"] as string,
                                        Price = (decimal)reader["Price"],
                                        CategoryName = reader["CategoryName"].ToString() ?? string.Empty
                                    };
                                }
                            }
                        }
                    }

                    if (product == null)
                    {
                        _logger.LogWarning("Product with ID {ProductId} not found.", id);
                        var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                        await notFoundResponse.WriteStringAsync("Product not found.");
                        return notFoundResponse;
                    }

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "application/json");
                    string jsonResponse = JsonConvert.SerializeObject(product);
                    await response.WriteStringAsync(jsonResponse);
                    return response;
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "A database error occurred while retrieving the product with ID {ProductId}.", id);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred in GetProductById for ProductId: {ProductId}.", id);
                    throw;
                }
            }
        }
    }
}
