using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Models
{
    public class AudioDeviceInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public bool IsDefault { get; set; }

        public override string ToString() => IsDefault ? $"{Name} (Default)" : Name;
    }
}
