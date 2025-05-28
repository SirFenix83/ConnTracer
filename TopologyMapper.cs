using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConnTracer.Network.Topology
{
    public class TopologyMapper
    {
        // Hostname/IP => TTL (geschätzte Hop-Anzahl)
        public Dictionary<IPAddress, int> HostTtlMap { get; } = new();

        // IP => MAC (ARP-Tabelle)
        public Dictionary<IPAddress, PhysicalAddress> ArpTable { get; private set; } = new();

        /// <summary>
        /// Ping mit TTL setzen, um Hops zu schätzen (Windows-only).
        /// </summary>
        public async Task<int> EstimateHopsAsync(IPAddress target)
        {
            try
            {
                using var ping = new Ping();
                var pingOptions = new PingOptions(ttl: 64, dontFragment: true);
                var buffer = new byte[32];
                var reply = await ping.SendPingAsync(target, 3000, buffer, pingOptions);

                if (reply.Status == IPStatus.Success)
                {
                    // TTL 64 ist Standard bei Windows, Unterschied zeigt Hop-Anzahl
                    int ttl = reply.Options?.Ttl ?? 0;
                    int hops = 64 - ttl;
                    return hops >= 0 ? hops : 0;
                }
            }
            catch
            {
                // Ignorieren bei Fehlern
            }
            return -1; // nicht erreichbar
        }

        /// <summary>
        /// Lädt die ARP-Tabelle aus dem System.
        /// </summary>
        public void LoadArpTable()
        {
            ArpTable = new Dictionary<IPAddress, PhysicalAddress>();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = "-a",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Beispielzeile:  192.168.1.1          00-14-22-01-23-45     dynamisch
            var regex = new Regex(@"(\d+\.\d+\.\d+\.\d+)\s+([\w-]+)\s+");

            foreach (Match match in regex.Matches(output))
            {
                if (IPAddress.TryParse(match.Groups[1].Value, out var ip) &&
                    PhysicalAddress.TryParse(match.Groups[2].Value.Replace('-', ':'), out var mac))
                {
                    ArpTable[ip] = mac;
                }
            }
        }

        /// <summary>
        /// Führt Traceroute für ein Ziel durch (Windows-only).
        /// </summary>
        public async Task<List<IPAddress>> TracerouteAsync(IPAddress target)
        {
            var route = new List<IPAddress>();
            try
            {
                for (int ttl = 1; ttl <= 30; ttl++)
                {
                    using var ping = new Ping();
                    var options = new PingOptions(ttl, true);
                    var buffer = new byte[32];
                    var reply = await ping.SendPingAsync(target, 3000, buffer, options);

                    if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.Success)
                    {
                        route.Add(reply.Address);
                        if (reply.Status == IPStatus.Success)
                            break;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch
            {
                // Fehler ignorieren
            }
            return route;
        }
    }
}
