using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ConnTracer.Services.Network
{
    public class TcpUploadTestServer : IDisposable
    {
        private readonly TcpListener listener;
        private CancellationTokenSource cts;

        public int Port { get; }

        public TcpUploadTestServer(int port)
        {
            Port = port;
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            if (cts != null && !cts.IsCancellationRequested)
                throw new InvalidOperationException("Server läuft bereits.");

            cts = new CancellationTokenSource();
            listener.Start();

            Task.Run(() => AcceptLoopAsync(cts.Token));
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(token);
                    _ = HandleClientAsync(client, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"TcpUploadTestServer Exception: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buffer = new byte[8192];
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                        if (bytesRead == 0)
                            break;
                    }
                }
                catch { }
            }
        }

        public void Stop()
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
                listener.Stop();
            }
        }

        public void Dispose()
        {
            Stop();
            cts?.Dispose();
        }
    }
}
