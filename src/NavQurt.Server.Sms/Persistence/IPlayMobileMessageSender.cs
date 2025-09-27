using NavQurt.Server.Sms.Models;

namespace NavQurt.Server.Sms.Persistence
{
    public interface IPlayMobileMessageSender
    {
        Task<PlayMobileResponse> SendSmsAsync(PlayMobileConfig playMobileConfig, SmsMessagesDto smsMessagesDto);
    }
}
