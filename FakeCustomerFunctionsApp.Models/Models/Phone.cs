namespace FakeCustomersFunctionApp.Models
{
    public class PhoneInputDto
    {
        public required string PhoneNumber { get; set; }
        public required string PhoneTypeName { get; set; }
    }

    public class PhoneResponseDto
    {
        public int PhoneId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PhoneType { get; set; }
    }
}
