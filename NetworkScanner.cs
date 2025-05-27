#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ConnTracer.Services.Core
{
    public class NetworkDevice
    {
        public string Name { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
    }

    public class TcpConnection
    {
        public string LocalEndPoint { get; set; } = string.Empty;
        public string RemoteEndPoint { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    public class NetworkScanner
    {
        public List<NetworkDevice> GetActiveNetworkDevices()
        {
            var devices = new List<NetworkDevice>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                var ipProps = ni.GetIPProperties();
                foreach (var ip in ipProps.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    var device = new NetworkDevice
                    {
                        Name = ni.Name,
                        IPAddress = ip.Address.ToString(),
                        MacAddress = FormatMacAddress(ni.GetPhysicalAddress())
                    };

                    devices.Add(device);
                }
            }

            return devices;
        }

        public async Task<List<NetworkDevice>> GetNetworkDataAsync()
        {
            // Nur eigene aktiven Netzwerk-Interfaces anzeigen (wie vorher)
            return await Task.Run(GetActiveNetworkDevices);
        }

        public List<TcpConnection> GetActiveConnections()
        {
            var tcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
            var result = new List<TcpConnection>();

            foreach (var conn in tcpConnections)
            {
                result.Add(new TcpConnection
                {
                    LocalEndPoint = conn.LocalEndPoint.ToString(),
                    RemoteEndPoint = conn.RemoteEndPoint.ToString(),
                    State = conn.State.ToString()
                });
            }

            return result;
        }

        public async Task<List<TcpConnection>> GetTcpConnectionsAsync()
        {
            return await Task.Run(GetActiveConnections);
        }

        private string FormatMacAddress(PhysicalAddress mac)
        {
            return string.Join(":", mac.GetAddressBytes().Select(b => b.ToString("X2")));
        }

        // Hilfsmethode für ARP-Lookup
        private string GetMacFromArp(string ipAddress)
        {
            try
            {
                var p = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "arp",
                        Arguments = "-a " + ipAddress,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains(ipAddress))
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                            return parts[1];
                    }
                }
            }
            catch { }
            return "";
        }
    }
}
