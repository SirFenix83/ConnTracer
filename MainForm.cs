using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ConnTracer.Services.Network;
using ConnTracer.Services.Core;
using System.Threading.Tasks;
using System.Collections.Generic;

#nullable disable

namespace ConnTracer
{
    public partial class MainForm : Form
    {
        private readonly DeviceScanner deviceScanner;
        private readonly NetworkScanner networkScanner;
        private readonly BandwidthAnalyzer bandwidthAnalyzer;
        private readonly BandwidthMonitor bandwidthMonitor;
        private readonly BandwidthTester bandwidthTester;
        private readonly System.Windows.Forms.Timer networkMonitorTimer;
        private readonly System.Windows.Forms.Timer bandwidthUpdateTimer;

        private int secondsUntilFirstBandwidthData = 5; // Countdown bis erste Bandbreitendaten
        private bool bandwidthMonitorDataReady = false;
        private bool bandwidthAnalyzerDataReady = false;
        private bool bandwidthTesterDataReady = false;

        public MainForm()
        {
            InitializeComponent();

            deviceScanner = new DeviceScanner();
            networkScanner = new NetworkScanner();
            bandwidthAnalyzer = new BandwidthAnalyzer();
            bandwidthMonitor = new BandwidthMonitor();
            bandwidthTester = new BandwidthTester();

            // DeviceScanner ListView Setup
            lvDeviceScanner.View = View.Details;
            lvDeviceScanner.FullRowSelect = true;
            lvDeviceScanner.Columns.Clear();
            lvDeviceScanner.Columns.Add("Device Name", 150);
            lvDeviceScanner.Columns.Add("IP Address", 120);
            lvDeviceScanner.Columns.Add("MAC Address", 150);
            lvDeviceScanner.Columns.Add("Manufacturer", 150);
            lvDeviceScanner.Columns.Add("Status", 100);

            // BandwidthOverview ListView Setup
            lvBandwidthOverview.View = View.Details;
            lvBandwidthOverview.FullRowSelect = true;
            lvBandwidthOverview.Columns.Clear();
            lvBandwidthOverview.Columns.Add("Kategorie", 200);
            lvBandwidthOverview.Columns.Add("Schnittstelle / Gerät", 300);
            lvBandwidthOverview.Columns.Add("Wert", 300);

            // Button Events
            btnShowBandwidthOverview.Click += BtnShowBandwidthOverview_Click;
            btnShowTcpConnections.Click += BtnShowTcpConnections_Click;
            btnShowNetworkMonitor.Click += BtnShowNetworkMonitor_Click;
            btnShowBottleneckAnalysis.Click += BtnShowBottleneckAnalysis_Click;
            btnShowDeviceScanner.Click += BtnShowDeviceScanner_Click;
            btnSaveLogs.Click += BtnSaveLogs_Click;
            btnTaskManager.Click += BtnTaskManager_Click;

            // Timer für Network Monitor - 5 Sekunden Intervall
            networkMonitorTimer = new System.Windows.Forms.Timer
            {
                Interval = 5_000
            };
            networkMonitorTimer.Tick += NetworkMonitorTimer_Tick;

            // Timer für Bandwidth Monitor Updates - 5 Sekunden Intervall
            bandwidthUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 5_000
            };
            bandwidthUpdateTimer.Tick += BandwidthUpdateTimer_Tick;

            bandwidthMonitor.OnBandwidthUpdate += BandwidthMonitor_OnBandwidthUpdate;

            ShowPanel(pnlBandwidthOverview);
            this.WindowState = FormWindowState.Maximized;

            bandwidthUpdateTimer.Start();
        }

        private async void BandwidthUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (secondsUntilFirstBandwidthData > 0)
            {
                ShowCountdown(secondsUntilFirstBandwidthData);
                secondsUntilFirstBandwidthData -= 5;
                return;
            }

            // Erstes Update für Monitor
            await bandwidthMonitor.MeasureAsync();
            bandwidthMonitorDataReady = true;

            // Dann Analyzer und Tester parallel starten, wenn nicht schon bereit
            if (!bandwidthAnalyzerDataReady) _ = UpdateBandwidthAnalyzerAsync();
            if (!bandwidthTesterDataReady) _ = UpdateBandwidthTesterAsync();

            UpdateBandwidthMonitorListView();
        }

        private void ShowCountdown(int secondsLeft)
        {
            lvBandwidthOverview.Items.Clear();
            lvBandwidthOverview.Items.Add(new ListViewItem(new[]
            {
                "Info",
                "Warte auf erste Daten...",
                $"Noch {secondsLeft} Sekunden"
            }));
        }

        private void BandwidthMonitor_OnBandwidthUpdate(string analysis)
        {
            if (lvBandwidthOverview.InvokeRequired)
            {
                lvBandwidthOverview.Invoke(new Action(UpdateBandwidthMonitorListView));
            }
            else
            {
                UpdateBandwidthMonitorListView();
            }
        }

        private void UpdateBandwidthMonitorListView()
        {
            // Alte Monitor-Einträge entfernen
            for (int i = lvBandwidthOverview.Items.Count - 1; i >= 0; i--)
            {
                if (lvBandwidthOverview.Items[i].SubItems[0].Text == "Monitor")
                    lvBandwidthOverview.Items.RemoveAt(i);
            }

            if (!bandwidthMonitorDataReady) return;

            var stats = bandwidthMonitor.GetCurrentStats();
            foreach (var kvp in stats)
            {
                lvBandwidthOverview.Items.Add(new ListViewItem(new[]
                {
                    "Monitor", kvp.Key, kvp.Value + " Kbps"
                }));
            }
        }

        private async Task UpdateBandwidthAnalyzerAsync()
        {
            for (int i = lvBandwidthOverview.Items.Count - 1; i >= 0; i--)
            {
                if (lvBandwidthOverview.Items[i].SubItems[0].Text == "Analyzer")
                    lvBandwidthOverview.Items.RemoveAt(i);
            }

            try
            {
                var analyzerData = await GetCurrentBandwidthUsageAsync();
                bandwidthAnalyzerDataReady = true;

                foreach (var kvp in analyzerData)
                {
                    lvBandwidthOverview.Items.Add(new ListViewItem(new[]
                    {
                        "Analyzer", kvp.Key, kvp.Value + " Kbps"
                    }));
                }
            }
            catch (Exception ex)
            {
                lvBandwidthOverview.Items.Add(new ListViewItem(new[]
                {
                    "Fehler", "Analyzer", ex.Message
                }));
            }
        }

        private async Task UpdateBandwidthTesterAsync()
        {
            for (int i = lvBandwidthOverview.Items.Count - 1; i >= 0; i--)
            {
                if (lvBandwidthOverview.Items[i].SubItems[0].Text == "Tester")
                    lvBandwidthOverview.Items.RemoveAt(i);
            }

            try
            {
                string testHost = "speedtest.net";
                var results = await bandwidthTester.RunTestAsync(testHost, 443, 5);

                bandwidthTesterDataReady = true;

                string downloadText = results.DownloadResult.Success
                    ? $"{results.DownloadResult.SpeedMbps:F2} Mbps"
                    : $"Fehler: {results.DownloadResult.Message}";

                lvBandwidthOverview.Items.Add(new ListViewItem(new[]
                {
                    "Tester", "Download", downloadText
                }));

                string uploadText = results.UploadResult.Success
                    ? $"{results.UploadResult.SpeedMbps:F2} Mbps"
                    : $"Fehler: {results.UploadResult.Message}";

                lvBandwidthOverview.Items.Add(new ListViewItem(new[]
                {
                    "Tester", "Upload", uploadText
                }));
            }
            catch (Exception ex)
            {
                lvBandwidthOverview.Items.Add(new ListViewItem(new[]
                {
                    "Fehler", "Tester", ex.Message
                }));
            }
        }

        private async Task<Dictionary<string, long>> GetCurrentBandwidthUsageAsync()
        {
            var before = bandwidthAnalyzer.GetCurrentBytes();
            await Task.Delay(5000);
            var after = bandwidthAnalyzer.GetCurrentBytes();

            return bandwidthAnalyzer.CalculateBandwidthUsage(before, after, 5.0);
        }

        private void BtnShowBandwidthOverview_Click(object sender, EventArgs e)
        {
            ShowPanel(pnlBandwidthOverview);
            // Reset Countdown und Ready Flags, um Live-Daten sauber neu zu starten
            secondsUntilFirstBandwidthData = 5;
            bandwidthMonitorDataReady = false;
            bandwidthAnalyzerDataReady = false;
            bandwidthTesterDataReady = false;
        }

        private void NetworkMonitorTimer_Tick(object sender, EventArgs e)
        {
            _ = UpdateNetworkMonitorAsync();
        }

        private async Task UpdateNetworkMonitorAsync()
        {
            if (lvNetworkMonitor == null) return;

            var devices = await networkScanner.GetNetworkDataAsync();

            lvNetworkMonitor.BeginInvoke(() =>
            {
                lvNetworkMonitor.Items.Clear();

                if (devices.Count == 0)
                {
                    lvNetworkMonitor.Items.Add(new ListViewItem(new[] { "Keine Geräte gefunden.", "", "" }));
                    return;
                }

                foreach (var device in devices)
                {
                    var item = new ListViewItem(new[]
                    {
                        device.Name,
                        device.IPAddress,
                        device.MacAddress
                    });
                    lvNetworkMonitor.Items.Add(item);
                }
            });
        }

        private void ShowPanel(Panel panelToShow)
        {
            pnlBandwidthOverview.Visible = false;
            pnlTcpConnections.Visible = false;
            pnlNetworkMonitor.Visible = false;
            pnlBottleneckAnalysis.Visible = false;
            pnlDeviceScanner.Visible = false;

            panelToShow.Visible = true;

            if (panelToShow == pnlNetworkMonitor)
            {
                networkMonitorTimer.Start();
                _ = UpdateNetworkMonitorAsync();
            }
            else
            {
                networkMonitorTimer.Stop();
            }
        }

        private void BtnShowTcpConnections_Click(object sender, EventArgs e) => ShowPanel(pnlTcpConnections);
        private void BtnShowNetworkMonitor_Click(object sender, EventArgs e) => ShowPanel(pnlNetworkMonitor);

        private void BtnShowBottleneckAnalysis_Click(object sender, EventArgs e)
        {
            ShowPanel(pnlBottleneckAnalysis);
            _ = UpdateBottleneckAnalysisAsync();
        }

        private async Task UpdateBottleneckAnalysisAsync()
        {
            if (lvBottleneckAnalysis == null) return;

            lvBottleneckAnalysis.Items.Clear();

            try
            {
                var usageData = await GetCurrentBandwidthUsageAsync();
                string result = bandwidthAnalyzer.DetectBottleneck(usageData);
                lvBottleneckAnalysis.Items.Add(new ListViewItem(new[] { "Bandwidth Check", result }));
            }
            catch (Exception ex)
            {
                lvBottleneckAnalysis.Items.Add(new ListViewItem(new[] { "Fehler", ex.Message }));
            }
        }

        private void BtnShowDeviceScanner_Click(object sender, EventArgs e)
        {
            ShowPanel(pnlDeviceScanner);
            _ = LoadDevicesAsync();
        }

        private async Task LoadDevicesAsync()
        {
            lvDeviceScanner.Items.Clear();

            string subnet = deviceScanner.GetLocalSubnet();
            var devices = await deviceScanner.ScanLocalNetworkAsync(subnet);

            if (devices.Count == 0)
            {
                lvDeviceScanner.Items.Add(new ListViewItem(new[] { "Keine Geräte gefunden.", "", "", "", "" }));
                return;
            }

            foreach (var device in devices)
            {
                var item = new ListViewItem(new[]
                {
                    device.Name,
                    device.IP,
                    device.MacAddress,
                    device.Manufacturer,
                    "Online"
                });
                lvDeviceScanner.Items.Add(item);
            }
        }

        private void BtnSaveLogs_Click(object sender, EventArgs e)
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Title = "Speicherort für Logdatei auswählen",
                    Filter = "Textdateien (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
                    FileName = "ConnTracer_Log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var sb = new StringBuilder();
                    foreach (ListViewItem item in lvBandwidthOverview.Items)
                    {
                        sb.AppendLine(string.Join("\t", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(x => x.Text)));
                    }
                    File.WriteAllText(sfd.FileName, sb.ToString());
                    MessageBox.Show("Logdatei erfolgreich gespeichert.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern der Logdatei: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTaskManager_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("taskmgr");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Task-Manager konnte nicht gestartet werden: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
