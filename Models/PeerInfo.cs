    using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Proximity.Models
{
    public class PeerInfo
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsOnline => (DateTime.UtcNow - LastSeen).TotalSeconds < 15;
    }
}
