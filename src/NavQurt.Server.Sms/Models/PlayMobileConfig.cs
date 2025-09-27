namespace NavQurt.Server.Sms.Models
{
	public class PlayMobileConfig
    {
		public PlayMobileConfig(string baseUrl, string login, string password, string originator)
		{
			BaseUrl = baseUrl;
			Login = login;
			Password = password;
			Originator = originator;
		}
		public string BaseUrl { get; set; } = default!;
		public string Login { get; set; } = default!;
		public string Password { get; set; } = default!;
		public string Originator { get; set; } = default!;
	}
}
