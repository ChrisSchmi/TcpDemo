using System;

namespace TcpDemo.Client
{
    public class DataReceivedEventArgs : EventArgs
    {
        public string Message { get; }

        public DataReceivedEventArgs(string message)
        {
            Message = message;
        }
    }
}