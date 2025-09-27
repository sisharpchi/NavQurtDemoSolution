using NavQurt.Server.Sms.Models;

namespace NavQurt.Server.Sms.Persistence
{
    public interface INavQurtmsSender
    {
        Task<bool> SendSms(SmsMessagesDto smsMessagesDto);
    }
}
