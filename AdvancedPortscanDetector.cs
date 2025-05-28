#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers; // explizit verwenden

namespace ConnTracer.Network.Security
{
    public class AdvancedPortscanDetector
    {
        public event Action<string>? OnPortscanDetected;

        private readonly ConcurrentDictionary<string, PortscanRecord> hostRecords = new();
        private readonly System.Timers.Timer cleanupTimer;

        private readonly TimeSpan timeWindow = TimeSpan.FromSeconds(10);
        private readonly int thresholdPorts = 20;
        private readonly int stealthThreshold = 5;

        public AdvancedPortscanDetector()
        {
            cleanupTimer = new System.Timers.Timer(30000);
            cleanupTimer.Elapsed += (_, _) => CleanupOldEntries();
            cleanupTimer.Start();
        }

        public void AnalyzePacket(string srcIp, int dstPort, string protocol, string flags)
        {
            var now = DateTime.UtcNow;

            var record = hostRecords.GetOrAdd(srcIp, _ => new PortscanRecord());

            lock (record)
            {
                record.History.Add(new PortActivity
                {
                    Timestamp = now,
                    Port = dstPort,
                    Protocol = protocol,
                    Flags = flags
                });

                var windowStart = now - timeWindow;
                var recentPorts = record.History
                    .Where(p => p.Timestamp >= windowStart)
                    .Select(p => p.Port)
                    .Distinct()
                    .Count();

                var stealthScanScore = record.History
                    .Where(p => p.Timestamp >= windowStart && IsStealthFlag(p.Flags))
                    .Count();

                if (recentPorts >= thresholdPorts || stealthScanScore >= stealthThreshold)
                {
                    var alert = $"Verdächtiger Scan von {srcIp}: " +
                                $"{recentPorts} Ports in {timeWindow.TotalSeconds}s, " +
                                $"Stealth-Score: {stealthScanScore}";
                    OnPortscanDetected?.Invoke(alert);
                    record.History.Clear(); // Reset nach Alarm
                }
            }
        }

        private void CleanupOldEntries()
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(1);

            foreach (var kvp in hostRecords)
            {
                lock (kvp.Value)
                {
                    kvp.Value.History.RemoveAll(p => p.Timestamp < cutoff);
                }
            }
        }

        private bool IsStealthFlag(string flags)
        {
            var stealthFlags = new[] { "FIN", "NULL", "XMAS" };
            return stealthFlags.Any(f => flags.Contains(f, StringComparison.OrdinalIgnoreCase));
        }
    }

    internal class PortscanRecord
    {
        public List<PortActivity> History { get; } = new();
    }

    internal class PortActivity
    {
        public DateTime Timestamp { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; } = "";
        public string Flags { get; set; } = "";
    }
}
