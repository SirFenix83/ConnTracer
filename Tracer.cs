#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ConnTracer.Services.Core
{
    public class Tracer
    {
        public class TraceResult
        {
            public int Hop { get; set; }
            public string Address { get; set; } = "*";
            public long RoundtripTime { get; set; } = -1;
            public bool TimedOut { get; set; }
        }

        /// <summary>
        /// Führt eine asynchrone Traceroute zu einem Host durch.
        /// </summary>
        /// <param name="host">Hostname oder IP</param>
        /// <param name="maxHops">Maximale Sprunganzahl</param>
        /// <param name="timeout">Timeout pro Ping in ms</param>
        /// <returns>Liste von Traceroute-Ergebnissen</returns>
        public async Task<List<TraceResult>> TraceRouteAsync(string host, int maxHops = 30, int timeout = 3000)
        {
            var traceResults = new List<TraceResult>();

            if (string.IsNullOrWhiteSpace(host))
                return traceResults;

            IPAddress? ipAddress;
            try
            {
                var entry = await Dns.GetHostEntryAsync(host);
                ipAddress = Array.Find(entry.AddressList, a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (ipAddress == null)
                    return traceResults;
            }
            catch
            {
                // DNS-Fehler
                return traceResults;
            }

            using var ping = new Ping();
            var buffer = Encoding.ASCII.GetBytes("traceroute-test");

            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                var options = new PingOptions(ttl, true);
                try
                {
                    var reply = await ping.SendPingAsync(ipAddress, timeout, buffer, options);

                    traceResults.Add(new TraceResult
                    {
                        Hop = ttl,
                        Address = reply.Address?.ToString() ?? "*",
                        RoundtripTime = reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.Success
                            ? reply.RoundtripTime
                            : -1,
                        TimedOut = reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.Success
                    });

                    if (reply.Status == IPStatus.Success)
                        break;
                }
                catch
                {
                    traceResults.Add(new TraceResult
                    {
                        Hop = ttl,
                        Address = "*",
                        RoundtripTime = -1,
                        TimedOut = true
                    });
                }
            }

            return traceResults;
        }
    }
}
