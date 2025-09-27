namespace NavQurt.Server.Application.Dto.Auth
{
    public class SignUpRequestDto
    {
        public string Phonenumber { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
