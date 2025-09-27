using System.Text.Json.Serialization;

namespace NavQurt.Server.Sms.Models
{
    internal class SmsSendMessage
    {
        public SmsSendMessage(int id, string body, string phoneNumber, string originator)
        {
            Id = id;
            Body = body;
            PhoneNumber = phoneNumber;
            Originator = originator;
        }

        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("body")]

        public string Body { get; set; }
        [JsonPropertyName("phone_numbers")]

        public string PhoneNumber { get; set; }

        [JsonPropertyName("originator")]

        public string Originator { get; set; }

    }
}
