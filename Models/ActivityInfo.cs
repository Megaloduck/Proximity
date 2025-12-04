using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Models;

public class ActivityInfo
{
    public string Icon { get; set; }
    public string Message { get; set; }
    public string TimeAgo { get; set; }
    public DateTime Timestamp { get; set; }
}
