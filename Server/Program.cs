using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpDemo.Server
{
    public class Program
    {
        public static async Task Main()
        {
            var server = new TcpEchoServer();
            await server.InitAsync(13000);
        }
    }
}
