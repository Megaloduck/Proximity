using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Proximity.Models
{
    public class DiscoveredPeer
    {
        public string Id { get; set; }
        public IPAddress Address { get; set; }
        public int ChatPort { get; set; }
        public int VoicePort { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
