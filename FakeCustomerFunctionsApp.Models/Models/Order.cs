namespace FakeCustomersFunctionApp.Models
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItemResponseDto>? OrderItems { get; set; }
    }

    public class OrderItemResponseDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CategoryName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderInputDto
    {
        public DateTime? OrderDate { get; set; }
        public List<OrderItemInputDto>? OrderItems { get; set; }
    }

    public class OrderItemInputDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }


    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public List<int>? OrderItemIds { get; set; }
    }


}
