namespace FakeCustomersFunctionApp.Models
{
    public class ProductResponseDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? CategoryName { get; set; } = string.Empty;
    }

    public class ProductInputDto
    {
        public required string ProductName { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public required string CategoryName { get; set; }
    }

    public class ProductInsertResponseDto
    {
        public int ProductId { get; set; }
    }
}
