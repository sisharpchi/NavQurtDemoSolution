using NavQurt.Server.Sms.Models;

namespace NavQurt.Server.Sms
{
    public static class Utils
    {
        public const string Originator = "Alipos";
        public static SmsMessagesDto Map(string id, string phoneNumber, string body, string originator)
        {
            return new SmsMessagesDto
            {
                Messages = new List<SmsMessageDto>
                {
                    new SmsMessageDto
                    {
                        MessageId = id,
                        Recipient =phoneNumber,
                        Sms = new SmsDto
                        {
                            Content = new SmsContent
                            {
                                Text = body,
                            },
                            Originator = originator
                        }
                    }
                }
            };
        }
    }
}
