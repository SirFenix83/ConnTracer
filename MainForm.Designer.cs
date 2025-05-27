namespace ConnTracer
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Button btnShowBandwidthOverview;
        private System.Windows.Forms.Button btnShowTcpConnections;
        private System.Windows.Forms.Button btnShowNetworkMonitor;
        private System.Windows.Forms.Button btnShowBottleneckAnalysis;
        private System.Windows.Forms.Button btnShowDeviceScanner;
        private System.Windows.Forms.Button btnSaveLogs;
        private System.Windows.Forms.Button btnTaskManager;

        private System.Windows.Forms.Panel pnlBandwidthOverview;
        private System.Windows.Forms.Panel pnlTcpConnections;
        private System.Windows.Forms.Panel pnlNetworkMonitor;
        private System.Windows.Forms.Panel pnlBottleneckAnalysis;
        private System.Windows.Forms.Panel pnlDeviceScanner;

        private System.Windows.Forms.ListView lvBandwidthOverview;
        private System.Windows.Forms.ListView lvTcpConnections;
        private System.Windows.Forms.ListView lvNetworkMonitor;
        private System.Windows.Forms.ListView lvDeviceScanner;
        private System.Windows.Forms.ListView lvBottleneckAnalysis;

        private System.Windows.Forms.Panel pnlBandwidthMonitor;
        private System.Windows.Forms.Panel pnlBandwidthAnalyzer;
        private System.Windows.Forms.Panel pnlBandwidthTester;

        private System.Windows.Forms.Label lblMonitorStatus;
        private System.Windows.Forms.Label lblAnalyzerStatus;
        private System.Windows.Forms.Label lblTesterStatus;

        private System.Windows.Forms.Timer timerBandwidthStatus;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // Buttons
            this.btnShowBandwidthOverview = new System.Windows.Forms.Button();
            this.btnShowTcpConnections = new System.Windows.Forms.Button();
            this.btnShowNetworkMonitor = new System.Windows.Forms.Button();
            this.btnShowBottleneckAnalysis = new System.Windows.Forms.Button();
            this.btnShowDeviceScanner = new System.Windows.Forms.Button();
            this.btnSaveLogs = new System.Windows.Forms.Button();
            this.btnTaskManager = new System.Windows.Forms.Button();

            // Panels (Hauptpanels)
            this.pnlBandwidthOverview = new System.Windows.Forms.Panel();
            this.pnlTcpConnections = new System.Windows.Forms.Panel();
            this.pnlNetworkMonitor = new System.Windows.Forms.Panel();
            this.pnlBottleneckAnalysis = new System.Windows.Forms.Panel();
            this.pnlDeviceScanner = new System.Windows.Forms.Panel();

            // ListViews (Hauptübersichten)
            this.lvBandwidthOverview = new System.Windows.Forms.ListView();
            this.lvTcpConnections = new System.Windows.Forms.ListView();
            this.lvNetworkMonitor = new System.Windows.Forms.ListView();
            this.lvDeviceScanner = new System.Windows.Forms.ListView();
            this.lvBottleneckAnalysis = new System.Windows.Forms.ListView();

            // Bandwidth Tool Panels
            this.pnlBandwidthMonitor = new System.Windows.Forms.Panel();
            this.pnlBandwidthAnalyzer = new System.Windows.Forms.Panel();
            this.pnlBandwidthTester = new System.Windows.Forms.Panel();

            // Status Labels
            this.lblMonitorStatus = new System.Windows.Forms.Label();
            this.lblAnalyzerStatus = new System.Windows.Forms.Label();
            this.lblTesterStatus = new System.Windows.Forms.Label();

            // Timer
            this.timerBandwidthStatus = new System.Windows.Forms.Timer(this.components);

            // Form settings
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Name = "MainForm";
            this.Text = "ConnTracer";

            // Buttons
            this.btnShowBandwidthOverview.Location = new System.Drawing.Point(10, 10);
            this.btnShowBandwidthOverview.Size = new System.Drawing.Size(180, 35);
            this.btnShowBandwidthOverview.Text = "Bandwidth Overview";

            this.btnShowTcpConnections.Location = new System.Drawing.Point(200, 10);
            this.btnShowTcpConnections.Size = new System.Drawing.Size(150, 35);
            this.btnShowTcpConnections.Text = "TCP Connections";

            this.btnShowNetworkMonitor.Location = new System.Drawing.Point(360, 10);
            this.btnShowNetworkMonitor.Size = new System.Drawing.Size(150, 35);
            this.btnShowNetworkMonitor.Text = "Network Monitor";

            this.btnShowBottleneckAnalysis.Location = new System.Drawing.Point(520, 10);
            this.btnShowBottleneckAnalysis.Size = new System.Drawing.Size(150, 35);
            this.btnShowBottleneckAnalysis.Text = "Bottleneck Analysis";

            this.btnShowDeviceScanner.Location = new System.Drawing.Point(680, 10);
            this.btnShowDeviceScanner.Size = new System.Drawing.Size(150, 35);
            this.btnShowDeviceScanner.Text = "Device Scanner";

            this.btnSaveLogs.Location = new System.Drawing.Point(10, 650);
            this.btnSaveLogs.Size = new System.Drawing.Size(150, 35);
            this.btnSaveLogs.Text = "Save Logs";

            this.btnTaskManager.Location = new System.Drawing.Point(170, 650);
            this.btnTaskManager.Size = new System.Drawing.Size(150, 35);
            this.btnTaskManager.Text = "Open Task Manager";

            // Hauptpanels mit ListViews
            SetupPanel(pnlTcpConnections, lvTcpConnections, new[] { "Kategorie", "Details", "Wert" }, new[] { 200, 500, 400 });
            SetupPanel(pnlNetworkMonitor, lvNetworkMonitor, new[] { "Name", "IP-Adresse", "MAC-Adresse" }, new[] { 300, 300, 400 });
            SetupPanel(pnlBottleneckAnalysis, lvBottleneckAnalysis, new[] { "Check", "Ergebnis" }, new[] { 300, 700 });
            SetupPanel(pnlDeviceScanner, lvDeviceScanner, new[] { "Device Name", "IP Address", "MAC Address", "Manufacturer", "Status" }, new[] { 150, 120, 150, 150, 100 });

            // Bandwidth Overview Panel und Subpanels
            this.pnlBandwidthOverview.Location = new System.Drawing.Point(10, 60);
            this.pnlBandwidthOverview.Size = new System.Drawing.Size(1170, 580);
            this.pnlBandwidthOverview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            SetupSubBandwidthPanel(pnlBandwidthMonitor, "Bandwidth Monitor", new System.Drawing.Point(10, 10));
            SetupSubBandwidthPanel(pnlBandwidthAnalyzer, "Bandwidth Analyzer", new System.Drawing.Point(10, 200));
            SetupSubBandwidthPanel(pnlBandwidthTester, "Bandwidth Tester", new System.Drawing.Point(10, 390));

            SetupStatusLabel(lblMonitorStatus, pnlBandwidthMonitor);
            SetupStatusLabel(lblAnalyzerStatus, pnlBandwidthAnalyzer);
            SetupStatusLabel(lblTesterStatus, pnlBandwidthTester);

            // Timer Setup
            this.timerBandwidthStatus.Interval = 1000;
            this.timerBandwidthStatus.Enabled = true;

            // Controls hinzufügen
            this.Controls.Add(this.btnShowBandwidthOverview);
            this.Controls.Add(this.btnShowTcpConnections);
            this.Controls.Add(this.btnShowNetworkMonitor);
            this.Controls.Add(this.btnShowBottleneckAnalysis);
            this.Controls.Add(this.btnShowDeviceScanner);
            this.Controls.Add(this.btnSaveLogs);
            this.Controls.Add(this.btnTaskManager);

            this.Controls.Add(this.pnlBandwidthOverview);
            this.Controls.Add(this.pnlTcpConnections);
            this.Controls.Add(this.pnlNetworkMonitor);
            this.Controls.Add(this.pnlBottleneckAnalysis);
            this.Controls.Add(this.pnlDeviceScanner);

            this.pnlBandwidthOverview.Controls.Add(pnlBandwidthMonitor);
            this.pnlBandwidthOverview.Controls.Add(pnlBandwidthAnalyzer);
            this.pnlBandwidthOverview.Controls.Add(pnlBandwidthTester);

            this.ResumeLayout(false);
        }

        private void SetupPanel(System.Windows.Forms.Panel panel, System.Windows.Forms.ListView listView, string[] columnNames, int[] columnWidths)
        {
            panel.Location = new System.Drawing.Point(10, 60);
            panel.Size = new System.Drawing.Size(1170, 580);
            panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            listView.Dock = System.Windows.Forms.DockStyle.Fill;
            listView.View = System.Windows.Forms.View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.Columns.Clear();

            for (int i = 0; i < columnNames.Length; i++)
            {
                listView.Columns.Add(columnNames[i], columnWidths[i]);
            }

            panel.Controls.Add(listView);
            panel.Visible = false;
        }

        private void SetupSubBandwidthPanel(System.Windows.Forms.Panel panel, string title, System.Drawing.Point location)
        {
            panel.Location = location;
            panel.Size = new System.Drawing.Size(1150, 180);
            panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            var lblTitle = new System.Windows.Forms.Label();
            lblTitle.Text = title;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblTitle.Location = new System.Drawing.Point(10, 10);
            lblTitle.Size = new System.Drawing.Size(300, 25);
            panel.Controls.Add(lblTitle);
        }

        private void SetupStatusLabel(System.Windows.Forms.Label label, System.Windows.Forms.Panel parentPanel)
        {
            label.AutoSize = false;
            label.Size = new System.Drawing.Size(1120, 120);
            label.Location = new System.Drawing.Point(10, 45);
            label.Font = new System.Drawing.Font("Segoe UI", 9F);
            label.Text = "Status: Warten auf erste Daten...";
            label.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            parentPanel.Controls.Add(label);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
