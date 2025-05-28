using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers; // Explizit den richtigen Namespace verwenden

namespace ConnTracer.Network.AnomalyDetection
{
    public class AnomalyDetector
    {
        private readonly int windowSeconds;
        private readonly int bucketSizeSeconds;

        // Host/IP => HostStats
        private readonly ConcurrentDictionary<string, HostStats> hostStats = new();

        private readonly System.Timers.Timer timer; // Explizit den Namespace angeben

        // Event bei Anomalie: Host/IP, Beschreibung
        public event Action<string, string> OnAnomalyDetected;

        public AnomalyDetector(int windowSeconds = 60, int bucketSizeSeconds = 10)
        {
            if (windowSeconds <= 0 || bucketSizeSeconds <= 0 || bucketSizeSeconds > windowSeconds)
                throw new ArgumentException("Ungültige Zeitfenster-Einstellungen");

            this.windowSeconds = windowSeconds;
            this.bucketSizeSeconds = bucketSizeSeconds;

            timer = new System.Timers.Timer(bucketSizeSeconds * 1000); // Explizit den Namespace angeben
            timer.Elapsed += Timer_Elapsed;
        }

        public void Start() => timer.Start();

        public void Stop() => timer.Stop();

        public void AddTrafficData(string hostIp, double bytes)
        {
            var stats = hostStats.GetOrAdd(hostIp, _ => new HostStats(windowSeconds / bucketSizeSeconds));
            stats.AddData(bytes);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var kvp in hostStats)
            {
                string host = kvp.Key;
                HostStats stats = kvp.Value;

                double latest = stats.LatestValue;
                double zscore = stats.Stats.ZScore(latest);

                if (Math.Abs(zscore) > 3.0)
                {
                    OnAnomalyDetected?.Invoke(host, $"Anomalie im Traffic: Z-Score={zscore:F2} (Bytes={latest})");
                }
            }
        }

        private class HostStats
        {
            public RollingStats Stats { get; }
            private readonly Queue<double> buckets = new();

            public HostStats(int maxBuckets)
            {
                Stats = new RollingStats(maxBuckets);
            }

            public void AddData(double value)
            {
                buckets.Enqueue(value);
                Stats.Add(value);

                if (buckets.Count > StatsWindowSize)
                {
                    buckets.Dequeue();
                }
            }

            public double LatestValue => buckets.Count > 0 ? buckets.Peek() : 0;

            private int StatsWindowSize => StatsWindowSizeFallback;

            private const int StatsWindowSizeFallback = 6;
        }
    }
}
