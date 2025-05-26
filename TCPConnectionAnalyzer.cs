using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ConnTracer.Services.Network
{
    public static class TcpConnectionAnalyzer
    {
        public static async Task<List<TcpConnectionInfo>> GetActiveTcpConnectionsAsync()
        {
            return await Task.Run(() =>
            {
                var connections = new List<TcpConnectionInfo>();
                var table = GetExtendedTcpTable(true);

                foreach (var row in table)
                {
                    var processName = "Unbekannt";
                    try
                    {
                        processName = Process.GetProcessById(row.ProcessId).ProcessName;
                    }
                    catch
                    {
                        // Prozess evtl. nicht mehr aktiv, ignorieren
                    }

                    connections.Add(new TcpConnectionInfo
                    {
                        ProcessId = row.ProcessId,
                        ProcessName = processName,
                        LocalAddress = row.LocalAddress.ToString(),
                        LocalPort = row.LocalPort,
                        RemoteAddress = row.RemoteAddress.ToString(),
                        RemotePort = row.RemotePort,
                        State = row.State.ToString()
                    });
                }

                return connections;
            });
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TcpTableClass tblClass, uint reserved);

        private enum TcpTableClass
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        private static List<MibTcpRowOwnerPid> GetExtendedTcpTable(bool sorted)
        {
            int AF_INET = 2; // IPv4
            int bufferSize = 0;
            IntPtr tcpTablePtr = IntPtr.Zero;

            uint ret = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, sorted, AF_INET, TcpTableClass.TCP_TABLE_OWNER_PID_ALL, 0);
            tcpTablePtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                ret = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, sorted, AF_INET, TcpTableClass.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != 0)
                    throw new Exception("GetExtendedTcpTable failed with error: " + ret);

                var table = Marshal.PtrToStructure<MibTcpTableOwnerPid>(tcpTablePtr);
                var rows = new List<MibTcpRowOwnerPid>();

                long rowPtr = tcpTablePtr.ToInt64() + Marshal.SizeOf(table.dwNumEntries);
                int rowSize = Marshal.SizeOf(typeof(MibTcpRowOwnerPid));

                for (int i = 0; i < table.dwNumEntries; i++)
                {
                    IntPtr rowAddress = new IntPtr(rowPtr + i * rowSize);
                    var tcpRow = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowAddress);
                    rows.Add(tcpRow);
                }

                return rows;
            }
            finally
            {
                if (tcpTablePtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(tcpTablePtr);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcpTableOwnerPid
        {
            public uint dwNumEntries;
            // Following rows follow in memory - handled manually
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcpRowOwnerPid
        {
            public TcpState State;
            public uint LocalAddr;
            public uint LocalPortRaw;
            public uint RemoteAddr;
            public uint RemotePortRaw;
            public int ProcessId;

            public IPAddress LocalAddress => new IPAddress(LocalAddr);
            public int LocalPort => ntohs((ushort)(LocalPortRaw >> 16));
            public IPAddress RemoteAddress => new IPAddress(RemoteAddr);
            public int RemotePort => ntohs((ushort)(RemotePortRaw >> 16));
        }

        [DllImport("ws2_32.dll")]
        private static extern ushort ntohs(ushort netshort);
    }

    public class TcpConnectionInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string LocalAddress { get; set; }
        public int LocalPort { get; set; }
        public string RemoteAddress { get; set; }
        public int RemotePort { get; set; }
        public string State { get; set; }
    }
}
