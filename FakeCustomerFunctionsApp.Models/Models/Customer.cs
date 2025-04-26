namespace FakeCustomersFunctionApp.Models
{
    public class CustomerResponseDto
    {
        public int CustomerId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<AddressResponseDto>? Addresses { get; set; }
        public List<PhoneResponseDto>? Phones { get; set; }
        public List<OrderDto>? Orders { get; set; }
    }

    public class CustomerCompositeInputDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public List<AddressInputDto>? Addresses { get; set; }
        public List<PhoneInputDto>? Phones { get; set; }
        public List<OrderInputDto>? Orders { get; set; }
    }

    public class CustomerInsertCompositeResponseDto
    {
        public int CustomerId { get; set; }
        public List<int>? AddressIds { get; set; }
        public List<int>? PhoneIds { get; set; }
        public List<int>? OrderIds { get; set; }
    }

    public class CustomerInsertDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
    }

    public class CustomerDetailDto
    {
        public int CustomerId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
