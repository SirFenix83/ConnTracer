using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ConnTracer.Network
{
    public class PortscanDetectedEventArgs : EventArgs
    {
        public string SourceIp { get; set; }
        public int ConnectionAttempts { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PortscanDetector
    {
        private readonly int timeWindowSeconds;
        private readonly int threshold;
        private readonly int cleanupIntervalSeconds;
        private readonly Action<string> logAction;

        private readonly ConcurrentDictionary<string, List<DateTime>> connectionLog;
        private CancellationTokenSource cts;

        public event EventHandler<PortscanDetectedEventArgs> PortscanDetected;

        public PortscanDetector(
            int timeWindowSeconds = 10,
            int attemptThreshold = 30,
            int cleanupIntervalSeconds = 30,
            Action<string> log = null)
        {
            this.timeWindowSeconds = timeWindowSeconds;
            this.threshold = attemptThreshold;
            this.cleanupIntervalSeconds = cleanupIntervalSeconds;
            this.logAction = log;

            connectionLog = new ConcurrentDictionary<string, List<DateTime>>();
        }

        public void Start()
        {
            if (cts != null) return;

            cts = new CancellationTokenSource();
            Task.Run(() => CleanupLoop(cts.Token));
        }

        public void Stop()
        {
            cts?.Cancel();
            cts = null;
        }

        public void RegisterConnection(string sourceIp)
        {
            var now = DateTime.UtcNow;
            var timestamps = connectionLog.GetOrAdd(sourceIp, _ => new List<DateTime>());

            lock (timestamps)
            {
                timestamps.Add(now);
                timestamps.RemoveAll(t => (now - t).TotalSeconds > timeWindowSeconds);

                if (timestamps.Count >= threshold)
                {
                    logAction?.Invoke($"[Portscan] {sourceIp} - {timestamps.Count} Verbindungen in {timeWindowSeconds}s");

                    PortscanDetected?.Invoke(this, new PortscanDetectedEventArgs
                    {
                        SourceIp = sourceIp,
                        ConnectionAttempts = timestamps.Count,
                        Timestamp = now
                    });

                    timestamps.Clear(); // Reset zur Vermeidung von Spam
                }
            }
        }

        private async Task CleanupLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(cleanupIntervalSeconds), token);
                var cutoff = DateTime.UtcNow.AddSeconds(-timeWindowSeconds);

                foreach (var kvp in connectionLog)
                {
                    lock (kvp.Value)
                    {
                        kvp.Value.RemoveAll(t => t < cutoff);
                    }
                }
            }
        }
    }
}
