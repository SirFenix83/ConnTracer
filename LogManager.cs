using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ConnTracer.Logging
{
    public class LogManager
    {
        private readonly List<LogEntry> logEntries = new();
        private readonly object lockObj = new();
        private readonly Dictionary<string, DateTime> lastLogTimes = new();

        public void AddLog(LogEntry logEntry, Control invokeControl, BindingSource bindingSourceLogs)
        {
            lock (lockObj)
            {
                // Fehler-Filter: max. 1 Log pro identischer Fehler-Meldung alle 2 Sekunden
                if (logEntry.Level == "Error")
                {
                    if (lastLogTimes.TryGetValue(logEntry.Message, out DateTime lastTime) &&
                        (DateTime.Now - lastTime).TotalSeconds < 2)
                    {
                        return;
                    }
                    lastLogTimes[logEntry.Message] = DateTime.Now;
                }

                logEntries.Add(logEntry);

                void UpdateBinding()
                {
                    bindingSourceLogs.DataSource = null;
                    bindingSourceLogs.DataSource = new List<LogEntry>(logEntries);
                }

                if (invokeControl.InvokeRequired)
                {
                    invokeControl.Invoke(new Action(UpdateBinding));
                }
                else
                {
                    UpdateBinding();
                }
            }
        }

        public void SaveLogsToFile()
        {
            try
            {
                string filename = $"ConnTracer_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                using StreamWriter writer = new(filename);
                foreach (var log in logEntries)
                {
                    writer.WriteLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {log.Level}: {log.Message}");
                }

                MessageBox.Show($"Logs erfolgreich gespeichert:\n{filename}", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Logs:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Level { get; set; }
        public string Message { get; set; }
    }
}
