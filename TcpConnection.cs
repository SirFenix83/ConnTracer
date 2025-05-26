using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace ConnTracer.Services.Network
{
    public class TcpConnection
    {
        public enum MIB_TCP_STATE
        {
            CLOSED = 1,
            LISTENING = 2,
            SYN_SENT = 3,
            SYN_RCVD = 4,
            ESTABLISHED = 5,
            FIN_WAIT1 = 6,
            FIN_WAIT2 = 7,
            CLOSE_WAIT = 8,
            CLOSING = 9,
            LAST_ACK = 10,
            TIME_WAIT = 11,
            DELETE_TCB = 12
        }

        public MIB_TCP_STATE State { get; internal set; }
        public IPAddress LocalAddress { get; internal set; }
        public int LocalPort { get; internal set; }
        public IPAddress RemoteAddress { get; internal set; }
        public int RemotePort { get; internal set; }
        public int ProcessId { get; internal set; }
        public string ProcessName { get; internal set; }

        public override string ToString()
        {
            return $"{LocalAddress}:{LocalPort} -> {RemoteAddress}:{RemotePort} | {State} | PID: {ProcessId} ({ProcessName})";
        }

        #region PInvoke and helpers

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
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

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public MIB_TCP_STATE state;
            public uint localAddr;
            public uint localPort;
            public uint remoteAddr;
            public uint remotePort;
            public int owningPid;
        }

        public static List<TcpConnection> GetAllTcpConnections()
        {
            var tcpConnections = new List<TcpConnection>();
            int buffSize = 0;

            // Ermitteln der erforderlichen Puffergroesse
            uint ret = GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
            IntPtr buff = Marshal.AllocHGlobal(buffSize);

            try
            {
                ret = GetExtendedTcpTable(buff, ref buffSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != 0)
                    throw new Exception($"GetExtendedTcpTable failed with error {ret}");

                int rowStructSize = Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID));
                int numEntries = Marshal.ReadInt32(buff);

                IntPtr rowPtr = new IntPtr(buff.ToInt64() + 4); // Offset hinter die Anzahl

                for (int i = 0; i < numEntries; i++)
                {
                    var tcpRow = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);

                    var localIp = new IPAddress(tcpRow.localAddr);
                    var remoteIp = new IPAddress(tcpRow.remoteAddr);

                    int localPort = ntohs((ushort)(tcpRow.localPort >> 16));
                    int remotePort = ntohs((ushort)(tcpRow.remotePort >> 16));

                    string processName = "Unknown";
                    try
                    {
                        processName = Process.GetProcessById(tcpRow.owningPid).ProcessName;
                    }
                    catch
                    {
                        // Prozess evtl. beendet oder Zugriff verweigert
                    }

                    tcpConnections.Add(new TcpConnection
                    {
                        State = tcpRow.state,
                        LocalAddress = localIp,
                        LocalPort = localPort,
                        RemoteAddress = remoteIp,
                        RemotePort = remotePort,
                        ProcessId = tcpRow.owningPid,
                        ProcessName = processName
                    });

                    rowPtr = new IntPtr(rowPtr.ToInt64() + rowStructSize);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buff);
            }

            return tcpConnections;
        }

        private const int AF_INET = 2;

        private static ushort ntohs(ushort netshort)
        {
            return (ushort)(((netshort & 0xFF) << 8) | ((netshort >> 8) & 0xFF));
        }

        #endregion
    }
}
