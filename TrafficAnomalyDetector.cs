using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Timer = System.Threading.Timer; // Eindeutige Zuordnung des Timer-Typs
#nullable enable

namespace ConnTracer.Network
{
    public class TrafficMetric
    {
        public DateTime Timestamp { get; set; }
        public long BytesTransferred { get; set; }
        public int Port { get; set; }
        public string SourceIp { get; set; } = string.Empty; // Standardwert hinzugefügt
    }

    public class AnomalyEventArgs : EventArgs
    {
        public string Description { get; set; } = string.Empty; // Standardwert hinzugefügt
        public DateTime Timestamp { get; set; }
    }

    public class TrafficAnomalyDetector
    {
        private readonly ConcurrentQueue<TrafficMetric> metrics = new();
        private readonly Timer evaluationTimer; // System.Threading.Timer wird verwendet
        private readonly object syncLock = new();
        private readonly int evaluationIntervalMs;
        private readonly long byteThreshold;
        private readonly int windowSeconds;

        public event EventHandler<AnomalyEventArgs>? AnomalyDetected;

        public TrafficAnomalyDetector(int evaluationIntervalMs = 5000, int windowSeconds = 60, long byteThreshold = 1000000)
        {
            this.evaluationIntervalMs = evaluationIntervalMs;
            this.windowSeconds = windowSeconds;
            this.byteThreshold = byteThreshold;

            evaluationTimer = new Timer(EvaluateMetrics, null, evaluationIntervalMs, evaluationIntervalMs);
        }

        public void AddMetric(TrafficMetric metric)
        {
            metrics.Enqueue(metric);
        }

        private void EvaluateMetrics(object? state)
        {
            lock (syncLock)
            {
                var now = DateTime.UtcNow;
                var cutoff = now.AddSeconds(-windowSeconds);

                // Nur Metriken innerhalb des Fensters auswerten
                var recentMetrics = metrics.Where(m => m.Timestamp >= cutoff).ToList();

                // Alte Metriken entfernen
                while (metrics.TryPeek(out var oldest) && oldest.Timestamp < cutoff)
                {
                    metrics.TryDequeue(out _);
                }

                // Beispiel: Wenn Summe Bytes > Schwellenwert, Alarm
                var totalBytes = recentMetrics.Sum(m => m.BytesTransferred);
                if (totalBytes > byteThreshold)
                {
                    AnomalyDetected?.Invoke(this, new AnomalyEventArgs
                    {
                        Timestamp = DateTime.Now,
                        Description = $"Traffic Peak detected: {totalBytes} Bytes in last {windowSeconds} seconds"
                    });
                }
            }
        }

        public void Dispose()
        {
            evaluationTimer.Dispose();
        }
    }
}
