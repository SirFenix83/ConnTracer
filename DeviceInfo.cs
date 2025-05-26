#nullable disable
namespace ConnTracer.Helpers
{
    public class DeviceInfo
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public string Status { get; set; }
        public string MacAddress { get; set; }       // Für Erweiterungen
        public string Manufacturer { get; set; }    // Für Erweiterungen

        public DeviceInfo() { }

        public DeviceInfo(string name, string ip, string status)
        {
            Name = name;
            IP = ip;
            Status = status;
            MacAddress = "Unbekannt";
            Manufacturer = "Unbekannt";
        }

        public override string ToString()
        {
            return $"{Name} ({IP}) - Status: {Status}";
        }
    }
}
