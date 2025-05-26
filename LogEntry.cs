using System;

namespace ConnTracer.Models
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; }
        public string ProgramName { get; }
        public LogLevel Level { get; }
        public string Message { get; }
        public string SourceIP { get; }
        public string DestinationIP { get; }

        public LogEntry(DateTime timestamp, string programName, LogLevel level, string message, string sourceIP, string destinationIP)
        {
            Timestamp = timestamp;
            ProgramName = programName ?? throw new ArgumentNullException(nameof(programName));
            Level = level;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            SourceIP = sourceIP ?? "Unbekannt";
            DestinationIP = destinationIP ?? "Unbekannt";
        }

        public override string ToString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss} | {ProgramName} | {Level} | {Message} | Von: {SourceIP} -> Zu: {DestinationIP}";
        }
    }
}
