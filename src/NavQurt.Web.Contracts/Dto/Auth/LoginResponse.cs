namespace NavQurt.Web.Contracts.Dto.Auth
{
    public class LoginResponse
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Id { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
    }
}
