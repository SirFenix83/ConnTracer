using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ConnTracer.Network
{
    public class IcmpPingResult
    {
        public string Host { get; set; }
        public long RoundtripTime { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }

    public class IcmpMonitor
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly List<string> hosts;
        private readonly int intervalMs;
        private readonly int timeoutMs;
        private readonly int maxHistoryPerHost;
        private readonly Action<string> logAction;

        private readonly ConcurrentDictionary<string, List<IcmpPingResult>> pingHistories;

        public event EventHandler<IcmpPingResult> PingResultReceived;

        public IcmpMonitor(
            IEnumerable<string> targetHosts,
            int intervalMilliseconds = 1000,
            int timeoutMilliseconds = 2000,
            int maxHistory = 100,
            Action<string> log = null)
        {
            hosts = new List<string>(targetHosts);
            intervalMs = intervalMilliseconds;
            timeoutMs = timeoutMilliseconds;
            maxHistoryPerHost = maxHistory;
            logAction = log;
            pingHistories = new ConcurrentDictionary<string, List<IcmpPingResult>>();
        }

        public void Start()
        {
            if (cancellationTokenSource != null) return;

            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => MonitorLoop(cancellationTokenSource.Token));
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = null;
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            var ping = new Ping();

            while (!token.IsCancellationRequested)
            {
                foreach (var host in hosts)
                {
                    try
                    {
                        var reply = await ping.SendPingAsync(host, timeoutMs);

                        var result = new IcmpPingResult
                        {
                            Host = host,
                            RoundtripTime = reply.RoundtripTime,
                            Success = reply.Status == IPStatus.Success,
                            Status = reply.Status.ToString(),
                            Timestamp = DateTime.Now
                        };

                        StoreHistory(result);
                        PingResultReceived?.Invoke(this, result);
                    }
                    catch (Exception ex)
                    {
                        var errorResult = new IcmpPingResult
                        {
                            Host = host,
                            RoundtripTime = -1,
                            Success = false,
                            Status = $"Error: {ex.Message}",
                            Timestamp = DateTime.Now
                        };

                        logAction?.Invoke($"[ICMP ERROR] {host}: {ex.Message}");

                        StoreHistory(errorResult);
                        PingResultReceived?.Invoke(this, errorResult);
                    }
                }

                await Task.Delay(intervalMs, token);
            }
        }

        private void StoreHistory(IcmpPingResult result)
        {
            var history = pingHistories.GetOrAdd(result.Host, _ => new List<IcmpPingResult>());
            lock (history)
            {
                history.Add(result);
                if (history.Count > maxHistoryPerHost)
                    history.RemoveAt(0); // älteste löschen
            }
        }

        public List<IcmpPingResult> GetHistoryForHost(string host)
        {
            if (pingHistories.TryGetValue(host, out var history))
            {
                lock (history)
                {
                    return new List<IcmpPingResult>(history);
                }
            }
            return new List<IcmpPingResult>();
        }
    }
}

namespace ConnTracer.Services.Network
{
    public class ICMPMonitor
    {
        public event EventHandler<IcmpPacketEventArgs> IcmpPacketDetected;

        public void Start()
        {
            // Logik zum Starten des ICMP-Monitors
        }

        protected virtual void OnIcmpPacketDetected(IcmpPacketEventArgs e)
        {
            IcmpPacketDetected?.Invoke(this, e);
        }
    }

    public class IcmpPacketEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }
}
