using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace ConnTracer.Services.Network
{
    public static class TcpConnectionFetcher
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public TcpConnection.MIB_TCP_STATE state;
            public uint localAddr;
            public uint localPort;
            public uint remoteAddr;
            public uint remotePort;
            public uint owningPid;
        }

        private const int AF_INET = 2;

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            nint pTcpTable,
            ref int dwOutBufLen,
            bool sort,
            int ipVersion,
            TCP_TABLE_CLASS tblClass,
            uint reserved = 0);

        private enum TCP_TABLE_CLASS
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

        public static List<TcpConnection> GetAllTcpConnectionsWithProcess()
        {
            var tcpConnections = new List<TcpConnection>();
            var processNameCache = new Dictionary<int, string>();

            int bufferSize = 0;
            uint result = GetExtendedTcpTable(nint.Zero, ref bufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
            if (result != 0 && result != 122)
                throw new Exception($"GetExtendedTcpTable initial call failed with error {result}");

            nint tcpTablePtr = nint.Zero;

            try
            {
                tcpTablePtr = Marshal.AllocHGlobal(bufferSize);
                result = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
                if (result != 0)
                    throw new Exception($"GetExtendedTcpTable failed with error {result}");

                int rowSize = Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID));
                int numEntries = Marshal.ReadInt32(tcpTablePtr);

                nint rowPtr = nint.Add(tcpTablePtr, 4);

                for (int i = 0; i < numEntries; i++)
                {
                    var tcpRow = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);
                    int pid = (int)tcpRow.owningPid;

                    if (!processNameCache.TryGetValue(pid, out string processName))
                    {
                        processName = GetProcessNameById(pid);
                        processNameCache[pid] = processName;
                    }

                    var connection = new TcpConnection
                    {
                        State = tcpRow.state,
                        LocalAddress = new IPAddress(tcpRow.localAddr),
                        LocalPort = ntohs((ushort)tcpRow.localPort),
                        RemoteAddress = new IPAddress(tcpRow.remoteAddr),
                        RemotePort = ntohs((ushort)tcpRow.remotePort),
                        ProcessId = pid,
                        ProcessName = processName
                    };

                    tcpConnections.Add(connection);
                    rowPtr = nint.Add(rowPtr, rowSize);
                }
            }
            finally
            {
                if (tcpTablePtr != nint.Zero)
                    Marshal.FreeHGlobal(tcpTablePtr);
            }

            return tcpConnections;
        }

        private static ushort ntohs(ushort netPort)
            => (ushort)((netPort >> 8) | (netPort << 8));

        private static string GetProcessNameById(int pid)
        {
            try
            {
                return Process.GetProcessById(pid).ProcessName;
            }
            catch
            {
                return "Unbekannt";
            }
        }
    }
}
