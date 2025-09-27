namespace NavQurt.Server.Core.Entities
{
    public class NavQurtSmsMessage : IEntity<int>
    {
        public int Id { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public DateTime SendedAt { get; set; }
        public bool IsSuccess { get; set; }
        public string Body { get; set; } = default!;
        public string Token { get; set; } = default!;
    }
}
