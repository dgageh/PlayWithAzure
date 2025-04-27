using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bogus;
using Newtonsoft.Json;
using FakeCustomersFunctionApp.Models; 

namespace FakeDataConsoleApp
{
    class Program
    {
        private const string FunctionsBaseUrl = "https://wordcompleter.azure-api.net/fakecustdb/api/";

        static async Task Main(string[] args)
        {
            using var client = new HttpClient { BaseAddress = new Uri(FunctionsBaseUrl) };

            int numberOfFakeProducts = 0;
            Console.WriteLine($"Inserting {numberOfFakeProducts} fake products...");

            // Setup a Faker for ProductInputDto.
            var productFaker = new Faker<ProductInputDto>()
                .RuleFor(p => p.ProductName, f => f.Commerce.ProductName())
                .RuleFor(p => p.Description, f => f.Lorem.Sentence())
                .RuleFor(p => p.Price, f => f.Random.Decimal(1, 100))
                .RuleFor(p => p.CategoryName, f => f.Commerce.Categories(1)[0]);

            for (int i = 0; i < numberOfFakeProducts; i++)
            {
                var fakeProduct = productFaker.Generate();
                var response = await client.PostAsJsonAsync("products", fakeProduct);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var productInsertResponse = JsonConvert.DeserializeObject<ProductInsertResponseDto>(jsonResponse);
                    Console.WriteLine($"Inserted fake product with ID: {productInsertResponse?.ProductId}");
                }
                else
                {
                    Console.WriteLine($"Failed to insert fake product #{i + 1}. Status: {response.StatusCode}");
                }
            }

            int numberOfFakeCustomers = 500;
            Console.WriteLine($"Inserting {numberOfFakeCustomers} fake composite customers...");

            var customerFaker = new Faker<CustomerCompositeInputDto>()
                .RuleFor(c => c.FirstName, f => f.Name.FirstName())
                .RuleFor(c => c.LastName, f => f.Name.LastName())
                .RuleFor(c => c.Email, f => f.Internet.Email())
                .RuleFor(c => c.Addresses,
                    f => new Faker<AddressInputDto>()
                            .RuleFor(a => a.StreetAddress, f => f.Address.StreetAddress())
                            .RuleFor(a => a.ZipCode, f => f.Address.ZipCode())
                            .RuleFor(a => a.City, f => f.Address.City())
                            .RuleFor(a => a.State, f => f.Address.StateAbbr())
                            .Generate(f.Random.Int(1, 2))) // generate 1 or 2 addresses
                .RuleFor(c => c.Phones,
                    f => new Faker<PhoneInputDto>()
                            .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber())
                            .RuleFor(p => p.PhoneType, f => f.PickRandom(new[] { "Mobile", "Home", "Work" }))
                            .Generate(f.Random.Int(1, 2))) // generate 1 or 2 phone record
                .RuleFor(c => c.Orders,
                    f => new Faker<OrderInputDto>()
                            .RuleFor(o => o.OrderDate, f => f.Date.Past())
                            .RuleFor(o => o.OrderItems,
                                f => new Faker<OrderItemInputDto>()
                                        .RuleFor(oi => oi.ProductId, f => f.Random.Int(1, 200))
                                        .RuleFor(oi => oi.Quantity, f => f.Random.Int(1, 10))
                                        .RuleFor(oi => oi.UnitPrice, f => f.Random.Decimal(1, 100))
                                        .Generate(f.Random.Int(1, 10))) // 1 to 10 order items
                            .Generate(f.Random.Int(0, 5))); // generate 0 to 10 orders

            for (int i = 0; i < numberOfFakeCustomers; i++)
            {
                var fakeCustomer = customerFaker.Generate();
                var response = await client.PostAsJsonAsync("customers", fakeCustomer);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var customerResponse = JsonConvert.DeserializeObject<CustomerInsertCompositeResponseDto>(jsonResponse);
                    Console.WriteLine($"Inserted fake customer with ID: {customerResponse?.CustomerId}");
                }
                else
                {
                    Console.WriteLine($"Failed to insert fake customer #{i + 1}. Status: {response.StatusCode}");
                }
            }

            Console.WriteLine("Fake data generation complete.");
        }
    }
}