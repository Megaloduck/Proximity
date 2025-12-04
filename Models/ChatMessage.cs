using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Models
{
    public class ChatMessage
    {
        public string Type { get; set; } = "text";
        public string MessageId { get; set; }
        public string FromDeviceId { get; set; }
        public string FromDeviceName { get; set; }
        public string ToDeviceId { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
