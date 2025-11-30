using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsPrivate { get; set; }
        public string? RecipientId { get; set; }
    }
}
