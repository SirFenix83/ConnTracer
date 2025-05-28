using PacketDotNet;
using PacketDotNet.Ieee80211;
// using PacketDotNet.Ip; // Entfernt, da nicht vorhanden
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Linq;
using System.Text;

namespace ConnTracer.Network
{
    public class PacketInspector
    {
        private ICaptureDevice device;

        public event Action<string> OnProtocolInfo;

        public void Start()
        {
            var devices = CaptureDeviceList.Instance;
            if (devices.Count < 1)
            {
                throw new Exception("Keine Netzwerkkarte gefunden.");
            }

            // Wähle erste NIC, die kein Loopback ist und einen Namen hat
            device = devices.FirstOrDefault(d =>
                !d.Name.ToLower().Contains("loopback") && !string.IsNullOrEmpty(d.Name));

            if (device == null)
            {
                throw new Exception("Keine passende Netzwerkkarte gefunden.");
            }

            device.OnPacketArrival += Device_OnPacketArrival;

            try
            {
                device.Open(DeviceModes.Promiscuous, 1000); // DeviceModes statt DeviceMode
                device.StartCapture();
            }
            catch (Exception ex)
            {
                OnProtocolInfo?.Invoke($"Fehler beim Öffnen der Netzwerkkarte: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (device != null)
            {
                try
                {
                    device.StopCapture();
                    device.OnPacketArrival -= Device_OnPacketArrival;
                    device.Close();
                }
                catch (Exception ex)
                {
                    OnProtocolInfo?.Invoke($"Fehler beim Stoppen des Capture-Geräts: {ex.Message}");
                }
                finally
                {
                    device = null;
                }
            }
        }

        // Korrigierte Signatur für PacketArrivalEventHandler
        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            try
            {
                var rawPacket = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);
                var ip = rawPacket.Extract<IPv4Packet>();
                if (ip == null) return;

                var tcp = rawPacket.Extract<TcpPacket>();
                var udp = rawPacket.Extract<UdpPacket>();

                if (tcp != null)
                {
                    if (tcp.PayloadData != null && tcp.PayloadData.Length > 0)
                    {
                        ParseTcpPayload(ip.SourceAddress.ToString(), ip.DestinationAddress.ToString(), tcp.DestinationPort, tcp.PayloadData);
                    }
                }

                if (udp != null)
                {
                    if (udp.DestinationPort == 53) // DNS
                    {
                        var query = ParseDnsPayload(udp.PayloadData);
                        if (!string.IsNullOrEmpty(query))
                        {
                            OnProtocolInfo?.Invoke($"DNS Query: {query}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnProtocolInfo?.Invoke($"Fehler bei Paket: {ex.Message}");
            }
        }

        private void ParseTcpPayload(string srcIp, string dstIp, int dstPort, byte[] payload)
        {
            string data = Encoding.UTF8.GetString(payload);

            if (dstPort == 80 || data.StartsWith("GET") || data.StartsWith("POST"))
            {
                var hostLine = ExtractHttpHost(data);
                if (hostLine != null)
                {
                    OnProtocolInfo?.Invoke($"HTTP Request an {hostLine} von {srcIp}");
                }
            }

            if (dstPort == 443 && payload.Length > 5 && payload[0] == 0x16)
            {
                var sni = ExtractTlsSni(payload);
                if (!string.IsNullOrEmpty(sni))
                {
                    OnProtocolInfo?.Invoke($"TLS ClientHello mit SNI: {sni}");
                }
            }
        }

        private string ExtractHttpHost(string data)
        {
            var lines = data.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("Host:"))
                    return line.Substring(5).Trim();
            }
            return null;
        }

        private string ParseDnsPayload(byte[] payload)
        {
            try
            {
                int length = payload.Length;
                if (length < 13) return null;

                int questionCount = (payload[4] << 8) | payload[5];
                if (questionCount == 0) return null;

                int pos = 12;
                StringBuilder sb = new StringBuilder();
                while (payload[pos] != 0)
                {
                    int len = payload[pos++];
                    if (len == 0 || pos + len > payload.Length) break;
                    sb.Append(Encoding.ASCII.GetString(payload, pos, len)).Append('.');
                    pos += len;
                }
                return sb.ToString().TrimEnd('.');
            }
            catch
            {
                return null;
            }
        }

        private string ExtractTlsSni(byte[] data)
        {
            try
            {
                int pos = 0;

                // TLS Record Header
                pos += 5;

                // Handshake Header
                pos += 1 + 3 + 2 + 32;

                // Session ID
                int sessionIdLength = data[pos];
                pos += 1 + sessionIdLength;

                // Cipher Suites
                int cipherLen = (data[pos] << 8) | data[pos + 1];
                pos += 2 + cipherLen;

                // Compression
                int compLen = data[pos];
                pos += 1 + compLen;

                // Extensions Length
                int extLen = (data[pos] << 8) | data[pos + 1];
                pos += 2;

                while (pos + 4 < data.Length)
                {
                    int extType = (data[pos] << 8) | data[pos + 1];
                    int extSize = (data[pos + 2] << 8) | data[pos + 3];
                    pos += 4;
                    if (extType == 0x00) // Server Name
                    {
                        pos += 2; // SNI list length
                        int nameType = data[pos++];
                        int nameLen = (data[pos] << 8) | data[pos + 1];
                        pos += 2;
                        return Encoding.ASCII.GetString(data, pos, nameLen);
                    }
                    pos += extSize;
                }
            }
            catch { }
            return null;
        }
    }
}
