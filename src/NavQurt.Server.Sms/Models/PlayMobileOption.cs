namespace NavQurt.Server.Sms.Models
{
    public class PlayMobileOption
    {
        public static string Key => nameof(PlayMobileOption);

        public string Login { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string Url { get; set; } = default!;
        public string Originator { get; set; } = default!;
    }
}
