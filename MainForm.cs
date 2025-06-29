﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ConnTracer.Services.Network;
using ConnTracer.Services.Core;
using ConnTracer.Services.Security; // Hinzufügen der fehlenden using-Direktive
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;

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

        private bool bandwidthMonitorDataReady = false;
        private bool bandwidthAnalyzerDataReady = false;
        private bool bandwidthTesterDataReady = false;

        private DataGridView dgvStatusOverview;

        // Definition von mainPanel hinzufügen
        private Panel mainPanel;

        // Neue Monitore
        private UDPMonitor udpMonitor;
        private ICMPMonitor icmpMonitor;
        private PortscanDetector portscanDetector;
        private TrafficAnomalyDetector trafficAnomalyDetector;

        // Panels und ListViews für die neuen Monitore
        private Panel pnlSecurityOverview;
        private ListView lvSecurityOverview;

        public MainForm()
        {
            InitializeComponent();

            // Initialisierung von mainPanel
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(mainPanel);

            deviceScanner = new DeviceScanner();
            networkScanner = new NetworkScanner();
            bandwidthAnalyzer = new BandwidthAnalyzer();
            bandwidthMonitor = new BandwidthMonitor();
            bandwidthTester = new BandwidthTester();

            // Monitore initialisieren
            udpMonitor = new UDPMonitor();
            icmpMonitor = new ICMPMonitor();
            portscanDetector = new PortscanDetector();
            trafficAnomalyDetector = new TrafficAnomalyDetector();

            lvDeviceScanner.View = View.Details;
            lvDeviceScanner.FullRowSelect = true;
            lvDeviceScanner.Columns.Clear();
            lvDeviceScanner.Columns.Add("Device Name", 150);
            lvDeviceScanner.Columns.Add("IP Address", 120);
            lvDeviceScanner.Columns.Add("MAC Address", 150);
            lvDeviceScanner.Columns.Add("Manufacturer", 150);
            lvDeviceScanner.Columns.Add("Status", 100);

            lvBandwidthOverview.View = View.Details;
            lvBandwidthOverview.FullRowSelect = true;
            lvBandwidthOverview.Columns.Clear();
            lvBandwidthOverview.Columns.Add("Kategorie", 200);
            lvBandwidthOverview.Columns.Add("Schnittstelle / Gerät", 300);
            lvBandwidthOverview.Columns.Add("Wert", 300);

            lvTcpConnections.View = View.Details;
            lvTcpConnections.FullRowSelect = true;
            lvTcpConnections.Columns.Clear();
            lvTcpConnections.Columns.Add("Prozess", 200);
            lvTcpConnections.Columns.Add("Lokale Adresse", 150);
            lvTcpConnections.Columns.Add("Remote Adresse", 150);
            lvTcpConnections.Columns.Add("Status", 100);

            lvNetworkMonitor.View = View.Details;
            lvNetworkMonitor.FullRowSelect = true;
            lvNetworkMonitor.Columns.Clear();
            lvNetworkMonitor.Columns.Add("Gerät", 200);
            lvNetworkMonitor.Columns.Add("IP Adresse", 150);
            lvNetworkMonitor.Columns.Add("MAC Adresse", 150);

            lvBottleneckAnalysis.View = View.Details;
            lvBottleneckAnalysis.FullRowSelect = true;
            lvBottleneckAnalysis.Columns.Clear();
            lvBottleneckAnalysis.Columns.Add("Kategorie", 200);
            lvBottleneckAnalysis.Columns.Add("Ergebnis", 600);

            // Panel und ListView für Security-Events
            pnlSecurityOverview = new Panel { Dock = DockStyle.Fill };
            lvSecurityOverview = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true
            };
            lvSecurityOverview.Columns.Add("Zeit", 140);
            lvSecurityOverview.Columns.Add("Typ", 120);
            lvSecurityOverview.Columns.Add("Beschreibung", 600);
            pnlSecurityOverview.Controls.Add(lvSecurityOverview);

            btnShowBandwidthOverview.Click += BtnShowBandwidthOverview_Click;
            btnShowTcpConnections.Click += BtnShowTcpConnections_Click;
            btnShowNetworkMonitor.Click += BtnShowNetworkMonitor_Click;
            btnShowBottleneckAnalysis.Click += BtnShowBottleneckAnalysis_Click;
            btnShowDeviceScanner.Click += BtnShowDeviceScanner_Click;
            btnSaveLogs.Click += BtnSaveLogs_Click;
            btnTaskManager.Click += BtnTaskManager_Click;

            networkMonitorTimer = new System.Windows.Forms.Timer { Interval = 5_000 };
            networkMonitorTimer.Tick += NetworkMonitorTimer_Tick;

            bandwidthUpdateTimer = new System.Windows.Forms.Timer { Interval = 1_000 }; // 1 Sekunde
            bandwidthUpdateTimer.Tick += BandwidthUpdateTimer_Tick;

            bandwidthMonitor.OnBandwidthUpdate += BandwidthMonitor_OnBandwidthUpdate;

            bandwidthMonitor.Start();

            dgvStatusOverview = new DataGridView
            {
                Location = new System.Drawing.Point(900, 70),
                Size = new System.Drawing.Size(270, 120),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                ColumnHeadersVisible = false, // Spaltenüberschriften ausblenden, falls gewünscht
                ColumnCount = 2,
                EnableHeadersVisualStyles = false
            };
            dgvStatusOverview.Columns[0].Name = "Komponente";
            dgvStatusOverview.Columns[0].Width = 120;
            dgvStatusOverview.Columns[1].Name = "Status";
            dgvStatusOverview.Columns[1].Width = 120;
            dgvStatusOverview.Rows.Add("Monitor", "Warten...");
            dgvStatusOverview.Rows.Add("Analyzer", "Warten...");
            dgvStatusOverview.Rows.Add("Tester", "Warten...");
            dgvStatusOverview.ClearSelection();
            dgvStatusOverview.DefaultCellStyle.SelectionBackColor = dgvStatusOverview.DefaultCellStyle.BackColor;
            dgvStatusOverview.DefaultCellStyle.SelectionForeColor = dgvStatusOverview.DefaultCellStyle.ForeColor;
            pnlBandwidthOverview.Controls.Add(dgvStatusOverview);

            // Panel und ListView sichtbar machen
            pnlBandwidthOverview.Visible = true;
            lvBandwidthOverview.Visible = true;

            // Panel als aktives Panel setzen
            ShowPanel(pnlBandwidthOverview);

            lblMonitorStatus.Text = "Status: Warten auf erste Daten...";
            lblAnalyzerStatus.Text = "Status: Warten auf erste Daten...";
            lblTesterStatus.Text = "Status: Warten auf erste Daten...";

            // Automatische Größenanpassung aktivieren
            this.MinimumSize = new Size(800, 600); // Optional: Mindestgröße setzen

            // Panels und Controls ggf. in ein Layout-Panel einfügen
            // TableLayoutPanel für die Hauptstruktur (nur 1 Buttonreihe oben, 1 unten, Ausgabe dazwischen)
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));         // Obere Buttonzeile
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));    // Ausgabe-Bereich
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));         // Untere Buttonzeile

            // Obere Buttonzeile (nur 1 Zeile!)
            FlowLayoutPanel buttonPanelTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            buttonPanelTop.Controls.Add(btnShowDeviceScanner);
            buttonPanelTop.Controls.Add(btnShowBandwidthOverview);
            buttonPanelTop.Controls.Add(btnShowTcpConnections);
            buttonPanelTop.Controls.Add(btnShowNetworkMonitor);
            buttonPanelTop.Controls.Add(btnShowBottleneckAnalysis);

            // Button für Security-Events
            var btnShowSecurityOverview = new Button
            {
                Text = "Security-Events",
                AutoSize = true
            };
            btnShowSecurityOverview.Click += BtnShowSecurityOverview_Click;
            buttonPanelTop.Controls.Add(btnShowSecurityOverview);

            // Ausgabe-Bereich (Panel für alle Ausgabepanels)
            Panel outputPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            outputPanel.Controls.Add(pnlBandwidthOverview);
            outputPanel.Controls.Add(pnlTcpConnections);
            outputPanel.Controls.Add(pnlNetworkMonitor);
            outputPanel.Controls.Add(pnlBottleneckAnalysis);
            outputPanel.Controls.Add(pnlDeviceScanner);
            outputPanel.Controls.Add(pnlSecurityOverview);

            // Untere Buttonzeile (Save Logs & Task Manager)
            FlowLayoutPanel buttonPanelBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            buttonPanelBottom.Controls.Add(btnSaveLogs);
            buttonPanelBottom.Controls.Add(btnTaskManager);

            // Panels docken
            pnlBandwidthOverview.Dock = DockStyle.Fill;
            pnlTcpConnections.Dock = DockStyle.Fill;
            pnlNetworkMonitor.Dock = DockStyle.Fill;
            pnlBottleneckAnalysis.Dock = DockStyle.Fill;
            pnlDeviceScanner.Dock = DockStyle.Fill;
            pnlSecurityOverview.Dock = DockStyle.Fill;

            // Zusammenbau ins TableLayoutPanel
            mainLayout.Controls.Add(buttonPanelTop, 0, 0);
            mainLayout.Controls.Add(outputPanel, 0, 1);
            mainLayout.Controls.Add(buttonPanelBottom, 0, 2);

            // mainPanel aufräumen und neues Layout einfügen
            mainPanel.Controls.Clear();
            mainPanel.Controls.Add(mainLayout);

            // Optional: Padding setzen
            mainPanel.Padding = new Padding(0, 10, 0, 0); // 10 Pixel Abstand nach oben

            // Fenster nicht maximiert starten
            this.WindowState = FormWindowState.Normal;

            WindowState = FormWindowState.Maximized;
            bandwidthUpdateTimer.Start();

            // Event-Handler für Monitore
            udpMonitor.UdpPacketDetected += (s, e) =>
                AddSecurityEvent("UDP", e.Timestamp, e.Description);
            icmpMonitor.IcmpPacketDetected += (s, e) =>
                AddSecurityEvent("ICMP", e.Timestamp, e.Description);
            portscanDetector.PortscanDetected += (s, e) =>
                AddSecurityEvent("Portscan", e.Timestamp, e.Description);
            trafficAnomalyDetector.AnomalyDetected += (s, e) =>
                AddSecurityEvent("Anomalie", e.Timestamp, e.Description);

            // Monitore starten (ggf. asynchron)
            udpMonitor.Start();
            icmpMonitor.Start();
            portscanDetector.Start();
            trafficAnomalyDetector.Start();
        }

        private async void BandwidthUpdateTimer_Tick(object sender, EventArgs e)
        {
            // BandwidthMonitor Update
            await bandwidthMonitor.MeasureAsync();
            bandwidthMonitorDataReady = true;
            UpdateBandwidthMonitorListView();

            if (!bandwidthAnalyzerDataReady)
            {
                await UpdateBandwidthAnalyzerAsync();
            }

            if (!bandwidthTesterDataReady)
            {
                await UpdateBandwidthTesterAsync();
            }
        }

        private void BandwidthMonitor_OnBandwidthUpdate(string analysis)
        {
            Color color = analysis.Contains("Warnung") ? Color.Red :
                          analysis.Contains("Hohe") ? Color.Orange :
                          analysis.Contains("Mittlere") ? Color.Yellow : Color.LightGreen;
            if (dgvStatusOverview.InvokeRequired)
                dgvStatusOverview.Invoke(() => SetStatus("Monitor", analysis, color));
            else
                SetStatus("Monitor", analysis, color);

            if (lblMonitorStatus.InvokeRequired)
                lblMonitorStatus.Invoke(() => lblMonitorStatus.Text = $"Status: {analysis}");
            else
                lblMonitorStatus.Text = $"Status: {analysis}";

            if (lvBandwidthOverview.InvokeRequired)
                lvBandwidthOverview.Invoke(UpdateBandwidthMonitorListView);
            else
                UpdateBandwidthMonitorListView();
        }

        private void UpdateBandwidthMonitorListView()
        {
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

                // Status-Label aktualisieren
                string analysis = bandwidthAnalyzer.DetectBottleneck(analyzerData);
                Color color = analysis.Contains("Warnung") ? Color.Red :
                              analysis.Contains("Hohe") ? Color.Orange :
                              analysis.Contains("Mittlere") ? Color.Yellow : Color.LightGreen;
                if (dgvStatusOverview.InvokeRequired)
                    dgvStatusOverview.Invoke(() => SetStatus("Analyzer", analysis, color));
                else
                    SetStatus("Analyzer", analysis, color);

                if (lblAnalyzerStatus.InvokeRequired)
                    lblAnalyzerStatus.Invoke(() => lblAnalyzerStatus.Text = $"Status: {analysis}");
                else
                    lblAnalyzerStatus.Text = $"Status: {analysis}";

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
                if (lblAnalyzerStatus.InvokeRequired)
                    lblAnalyzerStatus.Invoke(() => lblAnalyzerStatus.Text = $"Status: Fehler: {ex.Message}");
                else
                    lblAnalyzerStatus.Text = $"Status: Fehler: {ex.Message}";
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

                string uploadText = results.UploadResult.Success
                    ? $"{results.UploadResult.SpeedMbps:F2} Mbps"
                    : $"Fehler: {results.UploadResult.Message}";

                lvBandwidthOverview.Items.Add(new ListViewItem(new[]
                {
                    "Tester", "Download", downloadText
                }));
                lvBandwidthOverview.Items.Add(new ListViewItem(new[]
                {
                    "Tester", "Upload", uploadText
                }));

                // Status-Label aktualisieren
                string testerStatus = results.DownloadResult.Success && results.UploadResult.Success
                    ? "Test erfolgreich."
                    : $"Fehler: {results.DownloadResult.Message} {results.UploadResult.Message}";
                Color testerColor = results.DownloadResult.Success && results.UploadResult.Success
                    ? Color.LightGreen
                    : Color.Red;
                if (dgvStatusOverview.InvokeRequired)
                    dgvStatusOverview.Invoke(() => SetStatus("Tester", testerStatus, testerColor));
                else
                    SetStatus("Tester", testerStatus, testerColor);

                if (lblTesterStatus.InvokeRequired)
                    lblTesterStatus.Invoke(() => lblTesterStatus.Text = $"Status: {testerStatus}");
                else
                    lblTesterStatus.Text = $"Status: {testerStatus}";
            }
            catch (Exception ex)
            {
                lvBandwidthOverview.Items.Add(new ListViewItem(new[]
                {
                    "Fehler", "Tester", ex.Message
                }));
                if (lblTesterStatus.InvokeRequired)
                    lblTesterStatus.Invoke(() => lblTesterStatus.Text = $"Status: Fehler: {ex.Message}");
                else
                    lblTesterStatus.Text = $"Status: Fehler: {ex.Message}";
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

        private void BtnShowNetworkMonitor_Click(object sender, EventArgs e)
        {
            ShowPanel(pnlNetworkMonitor);
            _ = UpdateNetworkMonitorAsync();
        }

        private void ShowPanel(Panel panelToShow)
        {
            pnlBandwidthOverview.Visible = false;
            pnlTcpConnections.Visible = false;
            pnlNetworkMonitor.Visible = false;
            pnlBottleneckAnalysis.Visible = false;
            pnlDeviceScanner.Visible = false;
            pnlSecurityOverview.Visible = false;

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

        private void BtnShowTcpConnections_Click(object sender, EventArgs e)
        {
            ShowPanel(pnlTcpConnections);
            _ = UpdateTcpConnectionsAsync();
        }

        private async Task UpdateTcpConnectionsAsync()
        {
            if (lvTcpConnections == null) return;

            lvTcpConnections.BeginInvoke(() =>
            {
                lvTcpConnections.Items.Clear();
            });

            try
            {
                var tcpConnections = await networkScanner.GetTcpConnectionsAsync();

                lvTcpConnections.BeginInvoke(() =>
                {
                    foreach (var conn in tcpConnections)
                    {
                        var item = new ListViewItem(new[]
                        {
                            "Unbekannt", // ProcessName is not available in TcpConnection
                            $"{conn.LocalEndPoint}",
                            $"{conn.RemoteEndPoint}",
                            conn.State
                        });
                        lvTcpConnections.Items.Add(item);
                    }
                });
            }
            catch (Exception ex)
            {
                lvTcpConnections.BeginInvoke(() =>
                {
                    lvTcpConnections.Items.Add(new ListViewItem(new[]
                    {
                        "Fehler", "", "", ex.Message
                    }));
                });
            }
        }

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

            var devices = await deviceScanner.ScanLocalNetworkAsync(deviceScanner.GetLocalSubnet());

            if (devices.Count == 0)
            {
                lvDeviceScanner.Items.Add(new ListViewItem(new[] { "Keine Geräte gefunden.", "", "", "", "" }));
                return;
            }

            foreach (var d in devices)
            {
                var item = new ListViewItem(new[]
                {
                    d.Name,
                    d.IP,
                    d.MacAddress,
                    d.Manufacturer,
                    d.Status
                });
                lvDeviceScanner.Items.Add(item);
            }
        }

        private void BtnSaveLogs_Click(object sender, EventArgs e)
        {
            try
            {
                string logContent = ExportLogs();

                SaveFileDialog sfd = new SaveFileDialog()
                {
                    Filter = "Textdatei|*.txt",
                    FileName = $"ConnTracer_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Title = "Logs speichern"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, logContent, Encoding.UTF8);
                    MessageBox.Show("Logs wurden erfolgreich gespeichert.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Logs:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ExportLogs()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("=== TCP Connections ===");
            foreach (ListViewItem item in lvTcpConnections.Items)
            {
                sb.AppendLine(string.Join(" | ", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text)));
            }

            sb.AppendLine("\n=== Network Monitor ===");
            foreach (ListViewItem item in lvNetworkMonitor.Items)
            {
                sb.AppendLine(string.Join(" | ", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text)));
            }

            sb.AppendLine("\n=== Bottleneck Analysis ===");
            foreach (ListViewItem item in lvBottleneckAnalysis.Items)
            {
                sb.AppendLine(string.Join(" | ", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text)));
            }

            sb.AppendLine("\n=== Security Events ===");
            foreach (ListViewItem item in lvSecurityOverview.Items)
            {
                sb.AppendLine(string.Join(" | ", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text)));
            }

            return sb.ToString();
        }

        private void BtnTaskManager_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("taskmgr.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Starten des Task-Managers:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnShowSecurityOverview_Click(object sender, EventArgs e)
        {
            ShowPanel(pnlSecurityOverview);
        }

        // Beispielhafte Anpassung in AddSecurityEvent, um IPv6-Adressen korrekt darzustellen
        private void AddSecurityEvent(string typ, DateTime zeit, string beschreibung, string ipAdresse = null)
        {
            var values = new List<string>
            {
                zeit.ToString("yyyy-MM-dd HH:mm:ss"),
                typ,
                beschreibung
            };
            if (ipAdresse != null)
                values.Add(ipAdresse);

            if (lvSecurityOverview.InvokeRequired)
            {
                lvSecurityOverview.Invoke(() =>
                    lvSecurityOverview.Items.Insert(0, new ListViewItem(values.ToArray())));
            }
            else
            {
                lvSecurityOverview.Items.Insert(0, new ListViewItem(values.ToArray()));
            }
        }

        private void SetStatus(string component, string status, Color color)
        {
            foreach (DataGridViewRow row in dgvStatusOverview.Rows)
            {
                if (row.Cells[0].Value?.ToString() == component)
                {
                    row.Cells[1].Value = status;
                    row.Cells[1].Style.BackColor = color;
                    break;
                }
            }
        }
    }
}
