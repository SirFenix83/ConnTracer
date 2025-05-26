using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConnTracer.Services.Network
{
    public class BandwidthTester
    {
        public async Task<BandwidthTestResult> TestDownloadSpeedAsync(string host, int port = 443, int durationSeconds = 5)
        {
            byte[] buffer = new byte[8192];
            long totalBytes = 0;
            var stopwatch = new Stopwatch();

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port);
                using var stream = client.GetStream();

                stopwatch.Start();
                while (stopwatch.Elapsed.TotalSeconds < durationSeconds)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    totalBytes += bytesRead;
                }
                stopwatch.Stop();

                double megabits = totalBytes * 8 / (1024.0 * 1024.0);
                double speedMbps = megabits / stopwatch.Elapsed.TotalSeconds;

                return new BandwidthTestResult
                {
                    SpeedMbps = speedMbps,
                    Success = true,
                    Message = "Download erfolgreich."
                };
            }
            catch (Exception ex)
            {
                return new BandwidthTestResult
                {
                    SpeedMbps = 0,
                    Success = false,
                    Message = $"Fehler beim Download: {ex.Message}"
                };
            }
        }

        public async Task<BandwidthTestResult> TestUploadSpeedAsync(string host, int port = 443, int durationSeconds = 5)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(new string('A', 8192));
            long totalBytes = 0;
            var stopwatch = new Stopwatch();

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port);
                using var stream = client.GetStream();

                stopwatch.Start();
                while (stopwatch.Elapsed.TotalSeconds < durationSeconds)
                {
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                    totalBytes += buffer.Length;
                }
                stopwatch.Stop();

                double megabits = totalBytes * 8 / (1024.0 * 1024.0);
                double speedMbps = megabits / stopwatch.Elapsed.TotalSeconds;

                return new BandwidthTestResult
                {
                    SpeedMbps = speedMbps,
                    Success = true,
                    Message = "Upload erfolgreich."
                };
            }
            catch (Exception ex)
            {
                return new BandwidthTestResult
                {
                    SpeedMbps = 0,
                    Success = false,
                    Message = $"Fehler beim Upload: {ex.Message}"
                };
            }
        }

        // NEU: Kombinierte Testmethode
        public async Task<(BandwidthTestResult DownloadResult, BandwidthTestResult UploadResult)> RunTestAsync(string host, int port = 443, int durationSeconds = 5)
        {
            var downloadResult = await TestDownloadSpeedAsync(host, port, durationSeconds);
            var uploadResult = await TestUploadSpeedAsync(host, port, durationSeconds);
            return (downloadResult, uploadResult);
        }
    }
}
