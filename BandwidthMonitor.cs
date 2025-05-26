using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConnTracer.Services.Network
{
    public class BandwidthMonitor : IDisposable
    {
        private Dictionary<string, long> currentStats;
        private readonly BandwidthAnalyzer analyzer;

        public event Action<string> OnBandwidthUpdate;

        public BandwidthMonitor()
        {
            analyzer = new BandwidthAnalyzer();
            currentStats = new Dictionary<string, long>();
        }

        public void Start()
        {
            // Nichts tun, Timer läuft im MainForm
        }

        public void Stop()
        {
            // Nichts tun, Timer läuft im MainForm
        }

        public Dictionary<string, long> GetCurrentStats() => currentStats;

        public async Task MeasureAsync()
        {
            try
            {
                var before = analyzer.GetCurrentBytes();
                await Task.Delay(1000); // 1 Sekunde zwischen Messungen ist okay

                var after = analyzer.GetCurrentBytes();

                currentStats = analyzer.CalculateBandwidthUsage(before, after, 1.0);

                string analysis = analyzer.DetectBottleneck(currentStats);

                OnBandwidthUpdate?.Invoke(analysis);
            }
            catch (Exception ex)
            {
                OnBandwidthUpdate?.Invoke($"Fehler bei der Bandbreitenmessung: {ex.Message}");
            }
        }

        public void Dispose() { }
    }
}