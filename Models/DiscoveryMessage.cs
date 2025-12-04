using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Models
{
    public class DiscoveryMessage
    {
        public string PeerId { get; set; }
        public string Name { get; set; }
        public string Avatar { get; set; }
        public string StatusMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
