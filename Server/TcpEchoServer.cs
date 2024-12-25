using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpDemo.Server
{
    public class TcpEchoServer
    {
        private ConcurrentBag<TcpClient> clients;
        private int _port;

        public TcpEchoServer()
        {
            clients = [];
        }

        public async Task InitAsync(int port)
        {
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            _port = port;
            var listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            Console.WriteLine($"Server gestartet und wartet auf Verbindungen auf Port {_port}...");

            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync();
                clients.Add(client);
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine($"Client verbunden.");
            Console.WriteLine($"{Environment.NewLine}");
            Console.WriteLine($"Verbundene Clients: {clients.Count}");
            
            var buffer = new byte[1024];
            var stream = client.GetStream();
            var clientCts = new CancellationTokenSource();
            var clientToken = clientCts.Token;

            try
            {
                while (!clientToken.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, clientToken);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client hat die Verbindung geschlossen.");
                        clientCts.Cancel();
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Empfangen: {message}");

                    // Broadcast the message to all connected clients
                    await BroadcastMessageAsync(message, client, clientToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler: {ex.Message}");
            }
            finally
            {
                clients.TryTake(out _);
                client.Close();
                client.Dispose();
                Console.WriteLine($"Client getrennt.");
                Console.WriteLine($"{Environment.NewLine}");
                Console.WriteLine($"Verbundene Clients: {clients.Count}");
            }
        }

        private async Task BroadcastMessageAsync(string message, TcpClient senderClient, CancellationToken cancellationToken)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            foreach (var client in clients)
            {
                if (client != senderClient && client.Connected == true)
                {
                    try
                    {
                        var stream = client.GetStream();
                        await stream.WriteAsync(data, 0, data.Length, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Fehler beim Senden der Nachricht an einen Client: {ex.Message}");
                    }
                }
            }
        }
    }
}