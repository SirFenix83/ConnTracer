using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConnTracer.Network
{
    public class UdpPacketReceivedEventArgs : EventArgs
    {
        public IPEndPoint RemoteEndPoint { get; set; }
        public int LocalPort { get; set; }
        public byte[] Data { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class UdpMonitor
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly int[] portsToMonitor;

        public event EventHandler<UdpPacketReceivedEventArgs> UdpPacketReceived;

        public UdpMonitor(params int[] ports)
        {
            portsToMonitor = ports ?? Array.Empty<int>();
        }

        public void Start()
        {
            if (cancellationTokenSource != null) return;

            cancellationTokenSource = new CancellationTokenSource();

            foreach (var port in portsToMonitor)
            {
                Task.Run(() => ListenOnPort(port, cancellationTokenSource.Token));
            }
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = null;
        }

        private async Task ListenOnPort(int port, CancellationToken cancellationToken)
        {
            using var udpClient = new UdpClient(port);
            udpClient.Client.ReceiveTimeout = 500;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await udpClient.ReceiveAsync();

                        UdpPacketReceived?.Invoke(this, new UdpPacketReceivedEventArgs
                        {
                            RemoteEndPoint = result.RemoteEndPoint,
                            LocalPort = port,
                            Data = result.Buffer,
                            Timestamp = DateTime.Now
                        });
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        // Timeout ist okay – wir warten weiter
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[UDP Monitor] Fehler auf Port {port}: {ex.Message}");
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignorieren – passiert beim Stop
            }
        }
    }
}

namespace ConnTracer.Services.Network
{
    public class UDPMonitor
    {
        public event EventHandler<UdpPacketEventArgs> UdpPacketDetected;

        public void Start()
        {
            // Implementierung des Start-Logik
        }

        protected void OnUdpPacketDetected(UdpPacketEventArgs e)
        {
            UdpPacketDetected?.Invoke(this, e);
        }
    }

    public class UdpPacketEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }
}
