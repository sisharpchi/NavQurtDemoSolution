using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NavQurt.Server.Sms.Models
{
    public class SmsMessage
    {
        public SmsMessage(int id, string body, string phoneNumber)
        {
            Id = id;
            Body = body;
            PhoneNumber = phoneNumber;
        }
        public int Id { get; set; }
        public string Body { get; set; }
        public string PhoneNumber { get; set; }
    }
}
