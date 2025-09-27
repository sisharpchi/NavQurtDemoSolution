using NavQurt.Server.Sms.Models;
using NavQurt.Server.Sms.Persistence;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace NavQurt.Server.Sms.Services
{
    public class PlayMobileMessageSender : IPlayMobileMessageSender
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PlayMobileMessageSender(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PlayMobileResponse> SendSmsAsync(PlayMobileConfig playMobileConfig, SmsMessagesDto smsMessagesDto)
        {
            if (string.IsNullOrWhiteSpace(playMobileConfig.BaseUrl))
            {
                throw new ArgumentNullException("playMobileConfig.BaseUrl is null");
            }

            try
            {
                var json = JsonSerializer.Serialize(smsMessagesDto);
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(playMobileConfig.BaseUrl);
                var authString = $"{playMobileConfig.Login}:{playMobileConfig.Password}";
                var authBase64String = Convert.ToBase64String(Encoding.ASCII.GetBytes(authString));

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authBase64String);

                var response = await client.PostAsJsonAsync("", smsMessagesDto, CancellationToken.None);
                if (response.StatusCode != HttpStatusCode.OK)
                    return new PlayMobileResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = response.ReasonPhrase
                    };
                return PlayMobileResponse.Success;
            }
            catch (Exception exception)
            {
                return new PlayMobileResponse
                {
                    IsSuccess = false,
                    ErrorMessage = exception.Message
                };
            }
        }
    }
}
