using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Proximity.Services;

public class PingService
{
    public async Task<long> PingAsync(string ipAddress)
    {
        try
        {
            using var ping = new Ping();
            var stopwatch = Stopwatch.StartNew();

            var reply = await ping.SendPingAsync(ipAddress, 2000); // 2 second timeout

            stopwatch.Stop();

            if (reply.Status == IPStatus.Success)
            {
                return reply.RoundtripTime;
            }

            return -1; // Failed
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ping error: {ex.Message}");
            return -1;
        }
    }

    public async Task<Dictionary<string, long>> PingMultipleAsync(List<string> ipAddresses)
    {
        var results = new Dictionary<string, long>();
        var tasks = ipAddresses.Select(async ip =>
        {
            var latency = await PingAsync(ip);
            return (ip, latency);
        });

        var completedTasks = await Task.WhenAll(tasks);

        foreach (var (ip, latency) in completedTasks)
        {
            results[ip] = latency;
        }

        return results;
    }
}
