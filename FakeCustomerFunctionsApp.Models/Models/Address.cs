namespace FakeCustomersFunctionApp.Models
{
    public class AddressResponseDto
    {
        public int AddressId { get; set; }
        public string? StreetAddress { get; set; }
        public string? ZipCode { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
    }

    public class AddressInputDto
    {
        public required string StreetAddress { get; set; }
        public required string ZipCode { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
    }
}
