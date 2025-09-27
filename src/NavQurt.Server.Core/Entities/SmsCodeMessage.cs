namespace NavQurt.Server.Core.Entities
{
    public class SmsCodeMessage : IEntity<int>
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = default!;
        public string Code { get; set; } = default!;
        public DateTime SendedAt { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
