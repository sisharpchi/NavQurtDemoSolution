using NavQurt.Server.Sms.Models;
using NavQurt.Server.Sms.Persistence;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace NavQurt.Server.Sms.Services
{
    public class AliposMessageSender : INavQurtmsSender
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AliposMessageSender> _logger;

        public AliposMessageSender(IHttpClientFactory httpClientFactory,
            ILogger<AliposMessageSender> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        public async Task<bool> SendSms(SmsMessagesDto smsMessagesDto)
        {
            var httpClient = _httpClientFactory.CreateClient("AliposSms");
            try
            {
                var response = await httpClient.PostAsJsonAsync("", smsMessagesDto, CancellationToken.None);
                return response.IsSuccessStatusCode;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
                return false;
            }
        }
    }
}
