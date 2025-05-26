namespace ConnTracer.Services.Network
{
    public class BandwidthTestResult
    {
        public double SpeedMbps { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return Success
                ? $"Erfolg: {SpeedMbps:F2} Mbit/s"
                : $"Fehler: {Message}";
        }
    }
}