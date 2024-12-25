using System;
using System.Net;

namespace TcpDemo.Client
{
    public class ConnectedEventArgs : EventArgs
    {
        public ConnectedEventArgs(IPAddress ipAddress, int port)
        {
            IpAddress = ipAddress;
            Port = port;
        }

        public IPAddress IpAddress { get; }
        public int Port { get; }
    }
}