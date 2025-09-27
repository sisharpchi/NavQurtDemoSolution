namespace NavQurt.Server.Sms.Models
{
	public class PlayMobileResponse
	{
		public static PlayMobileResponse Success => new PlayMobileResponse { IsSuccess= true };
		public bool IsSuccess { get; set; }
		public string? ErrorMessage { get; set; }
	}
}
