#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConnTracer.Helpers;  // WICHTIG: Damit DeviceInfo mit Status genutzt wird

namespace ConnTracer.Services.Network
{
    public class DeviceScanner
    {
        private Dictionary<string, string> ouiDatabase;

        public DeviceScanner()
        {
            // Lokale Datei bevorzugen
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "oui.txt");
            if (System.IO.File.Exists(path))
                System.Diagnostics.Debug.WriteLine("OUI-Datei gefunden!");
            else
                System.Diagnostics.Debug.WriteLine("OUI-Datei NICHT gefunden!");

            if (System.IO.File.Exists(path))
                ouiDatabase = LoadOuiDatabaseFromFile(path);
            else
                ouiDatabase = LoadOuiDatabase(); // Fallback auf kleine Liste
        }

        public async Task<List<DeviceInfo>> ScanLocalNetworkAsync(string subnet)
        {
            var activeDevices = new List<DeviceInfo>();
            var tasks = new List<Task>();

            for (int i = 1; i < 255; i++)
            {
                string ip = $"{subnet}.{i}";
                tasks.Add(Task.Run(async () =>
                {
                    using var ping = new Ping();
                    try
                    {
                        var reply = await ping.SendPingAsync(ip, 250);
                        if (reply.Status == IPStatus.Success)
                        {
                            string hostName = ip;
                            try
                            {
                                var entry = await Dns.GetHostEntryAsync(ip);
                                hostName = entry.HostName;
                            }
                            catch { /* Hostname nicht auflösbar, IP als Fallback */ }

                            string mac = GetMacAddress(ip);
                            string manufacturer = GetManufacturer(mac);

                            var device = new DeviceInfo
                            {
                                IP = ip,
                                Name = hostName,
                                Status = "Online",
                                MacAddress = mac,
                                Manufacturer = manufacturer
                            };

                            lock (activeDevices)
                            {
                                activeDevices.Add(device);
                            }
                        }
                        else
                        {
                            // Optional: Geräte, die nicht antworten, kannst du hier hinzufügen mit Status "Offline" oder weglassen
                        }
                    }
                    catch
                    {
                        // Fehler ignorieren, kein Gerät hinzufügen
                    }
                }));
            }

            await Task.WhenAll(tasks);

            return activeDevices
                .OrderBy(d => IPAddress.Parse(d.IP).GetAddressBytes(), new ByteArrayComparer())
                .ToList();
        }

        private string GetMacAddress(string ip)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var output = RunCommand("arp", "-a " + ip);
                    var regex = new Regex(@"(([0-9A-Fa-f]{2}[-:]){5}[0-9A-Fa-f]{2})");
                    var match = regex.Match(output);
                    if (match.Success)
                        return match.Value.ToUpper();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                         RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var output = RunCommand("arp", "-n " + ip);
                    var regex = new Regex(@"(([0-9a-f]{2}:){5}[0-9a-f]{2})");
                    var match = regex.Match(output);
                    if (match.Success)
                        return match.Value.ToUpper();
                }
            }
            catch { }
            return "Unbekannt";
        }

        private Dictionary<string, string> LoadOuiDatabase()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "00-1A-2B", "Cisco Systems" },
                { "00-1B-63", "Apple, Inc." },
                { "00-0C-29", "VMware, Inc." },
                { "00-50-56", "VMware, Inc." },
                { "00-15-5D", "Microsoft Corporation" },
                { "F4-5C-89", "Intel Corporate" },
                { "00-1C-B3", "Samsung Electronics" },
                { "00-21-6A", "Hewlett Packard" },
                { "00-24-E8", "Hon Hai Precision Ind. Co.,Ltd." },
            };
        }

        private async Task<Dictionary<string, string>> DownloadOuiDatabaseAsync()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var client = new HttpClient();
            var content = await client.GetStringAsync("https://standards-oui.ieee.org/oui/oui.txt");
            var regex = new Regex(@"^(?<prefix>[0-9A-F]{6})\s+\(base 16\)\s+(?<vendor>.+)$", RegexOptions.Multiline);

            foreach (Match match in regex.Matches(content))
            {
                string prefix = match.Groups["prefix"].Value.ToUpper();
                // Format zu XX-XX-XX
                prefix = $"{prefix.Substring(0,2)}-{prefix.Substring(2,2)}-{prefix.Substring(4,2)}";
                string vendor = match.Groups["vendor"].Value.Trim();
                dict[prefix] = vendor;
            }
            return dict;
        }

        private Dictionary<string, string> LoadOuiDatabaseFromFile(string path)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var content = System.IO.File.ReadAllText(path);
            var regex = new Regex(@"^(?<prefix>[0-9A-F]{6})\s+\(base 16\)\s+(?<vendor>.+)$", RegexOptions.Multiline);

            foreach (Match match in regex.Matches(content))
            {
                string prefix = match.Groups["prefix"].Value.ToUpper();
                prefix = $"{prefix.Substring(0,2)}-{prefix.Substring(2,2)}-{prefix.Substring(4,2)}";
                string vendor = match.Groups["vendor"].Value.Trim();
                dict[prefix] = vendor;
            }
            return dict;
        }

        private string GetManufacturer(string mac)
        {
            if (string.IsNullOrEmpty(mac) || mac == "Unbekannt" || mac.Length < 8)
                return "Unbekannt";

            // MAC-Präfix auf einheitliches Format bringen (Bindestriche, Großbuchstaben, 3 Blöcke)
            string prefix = mac.Substring(0, 8).Replace(':', '-').ToUpper();

            System.Diagnostics.Debug.WriteLine($"MAC: {mac}, Prefix: {prefix}");

            // Falls das Präfix z.B. "F4-5C-89" ist, aber mac "F4-5C-89-12-34-56", dann stimmt das Format
            if (ouiDatabase.TryGetValue(prefix, out var manufacturer))
                return manufacturer;

            // Alternativ: Versuche die ersten 8 Zeichen mit nur Bindestrichen
            prefix = string.Join("-", mac.Split('-', ':').Take(3));
            if (ouiDatabase.TryGetValue(prefix, out manufacturer))
                return manufacturer;

            return "Unbekannt";
        }

        private string RunCommand(string command, string args)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        public string GetLocalSubnet()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            var mask = ua.IPv4Mask;
                            var ipBytes = ua.Address.GetAddressBytes();
                            var maskBytes = mask.GetAddressBytes();

                            var subnetBytes = new byte[4];
                            for (int i = 0; i < 4; i++)
                                subnetBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);

                            return string.Join(".", subnetBytes.Take(3));
                        }
                    }
                }
            }
            return "192.168.1";
        }

        private class ByteArrayComparer : IComparer<byte[]>
        {
            public int Compare(byte[] x, byte[] y)
            {
                for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
                {
                    int diff = x[i].CompareTo(y[i]);
                    if (diff != 0)
                        return diff;
                }
                return x.Length.CompareTo(y.Length);
            }
        }
    }
}
