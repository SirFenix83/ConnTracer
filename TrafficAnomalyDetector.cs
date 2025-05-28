using System;
using System.Timers; // Explizit den richtigen Namespace verwenden

namespace ConnTracer.Services.Security
{
    public class TrafficAnomalyDetector
    {
        public event EventHandler<AnomalyDetectedEventArgs> AnomalyDetected;

        private System.Timers.Timer _timer; // Explizit den Namespace angeben

        public void Start()
        {
            // Beispiel: Simuliere alle 10 Sekunden eine Anomalie
            _timer = new System.Timers.Timer(10000); // Explizit den Namespace angeben
            _timer.Elapsed += (s, e) =>
            {
                OnAnomalyDetected(new AnomalyDetectedEventArgs(DateTime.Now, "Beispiel-Anomalie erkannt"));
            };
            _timer.AutoReset = true;
            _timer.Start();
        }

        protected virtual void OnAnomalyDetected(AnomalyDetectedEventArgs e)
        {
            AnomalyDetected?.Invoke(this, e);
        }
    }

    public class AnomalyDetectedEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }

        public AnomalyDetectedEventArgs(DateTime timestamp, string description)
        {
            Timestamp = timestamp;
            Description = description;
        }
    }
}