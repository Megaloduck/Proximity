using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Proximity.Models
{
    public class DiscoveredPeer
    {
        public string Id { get; set; } = string.Empty;
        public IPAddress Address { get; set; } = IPAddress.None;
        public int ChatPort { get; set; }
        public int VoicePort { get; set; }
        public DateTime LastSeen { get; set; }

        // Profile fields
        public string DisplayName { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public string Emoji { get; set; } = "😀";
    }
}
