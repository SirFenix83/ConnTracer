🧭 Überblick
Repository: ConnTracer
Sprache: C#
Typ: Windows-Desktop-Anwendung
Ziel: Überwachung und Analyse von Netzwerkverbindungen

🔍 Funktionen im Detail
1. Netzwerkscanner
Zweck: Erkennt und listet alle aktiven Netzwerkgeräte im lokalen Netzwerk auf.

Technologie: Verwendung von System.Net.NetworkInformation zur Erkennung aktiver Hosts.

Abgerufene Werte: IP-Adressen, MAC-Adressen, Hostnamen (sofern verfügbar).

2. TCP-Verbindungsanalyse
Zweck: Überwacht und analysiert eingehende und ausgehende TCP-Verbindungen.

Technologie: Nutzung von System.Net.NetworkInformation.IPGlobalProperties und System.Net.NetworkInformation.TcpConnectionInformation.

Abgerufene Werte: Lokale und entfernte IP-Adressen, Ports, Verbindungsstatus (z. B. Established, Listen), Prozess-IDs (PID).

3. Bandbreitenüberwachung
Zweck: Ermittelt die aktuelle Bandbreitennutzung des Systems.

Technologie: Verwendung von PerformanceCounter zur Abfrage von Netzwerkstatistiken.

Abgerufene Werte: Gesendete und empfangene Bytes pro Sekunde, Paketanzahl.
GitHub

4. Protokollierung
Zweck: Speichert relevante Netzwerkereignisse und -daten für spätere Analysen.

Technologie: Einsatz von LogManager und LogEntry zur strukturierten Speicherung.

Abgerufene Werte: Zeitstempel, Ereignistyp (z. B. Verbindungsaufbau, Verbindungsabbruch), betroffene IP-Adressen und Ports.

📦 Struktur des Repositories
ConnTracer.csproj & ConnTracer.sln: Projektdateien für Visual Studio.

MainForm.cs & MainForm.Designer.cs: Benutzeroberfläche der Anwendung.

Program.cs: Einstiegspunkt der Anwendung.

DeviceScanner.cs: Logik zur Erkennung von Netzwerkgeräten.

TCPConnectionFetcher.cs: Abfrage und Verarbeitung von TCP-Verbindungsdaten.

BandwidthMonitor.cs: Überwachung der Bandbreitennutzung.

LogManager.cs & LogEntry.cs: Protokollierung und Speicherung von Ereignissen.
GitHub
