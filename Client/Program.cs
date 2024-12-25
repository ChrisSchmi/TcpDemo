using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpDemo.Client
{
    public class DemoTcpClient
    {   
        static async Task HandleInputs(ModernTcpClient client, CancellationToken cancellationToken)
        {
            Console.WriteLine($"{Environment.NewLine}");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"Please enter a message: ");

                    var input = Console.ReadLine();

                    if(input?.StartsWith('/') == true)
                    {
                        input = input.Replace("/","").ToLowerInvariant();

                        switch(input)
                        {
                            case "disconnect":
                                await DisconnectAsync(client);
                                input = string.Empty;
                                break;

                            case "exit":
                                await DisconnectAsync(client);
                                input = string.Empty;
                                break;

                            case "help":
                                ShowHelp();
                                input = string.Empty;
                                break;                                
                                
                            default:
                                Console.WriteLine($"Unknown Command: {input}");
                                Console.WriteLine($"{Environment.NewLine}");
                                input = string.Empty;
                                break;
                        }
                    }

                    if(string.IsNullOrWhiteSpace(input) == false)
                    {
                        await client.SendAsync(input);
                        Console.WriteLine($"Sent: {input}");
                        Console.WriteLine($"{Environment.NewLine}");
                    }
                    
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while sending: {ex.Message}");
                }
            }
        }
        static void ShowHelp()
        {
            Console.WriteLine($"{Environment.NewLine}");
            Console.WriteLine("exit         -   closes the connection and disconnects from the server.");
            Console.WriteLine("disconnect   -   closes the connection and disconnects from the server.");
            Console.WriteLine("help         -   displays this help message.");
            Console.WriteLine($"{Environment.NewLine}");
        }
        static async Task DisconnectAsync(ModernTcpClient client)
        {
            if(client.Connected == true)
            {
                await client.DisconnectAsync();
            }
        }
        static async Task Main()    
        {
            var client = new ModernTcpClient();
            int port = 13000;
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            
            client.OnDataReceived += (sender, e) => Console.WriteLine($"{Environment.NewLine}Received: {e.Message}");
            client.OnDisconnected += (sender, e) =>
            {
                Console.WriteLine("Server connection closed.");
                cts.Cancel();
            };

            client.OnConnected += (sender, e) => Console.WriteLine($"Connected to Server {e.IpAddress}:{e.Port}.");

            try
            {
                await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), port);          
            }
            catch (Exception ex)
            {
                cts.Cancel();
                Console.WriteLine($"Error on connecting: {ex.Message}");
                return;
            }
            
            await HandleInputs(client, cancellationToken);
        }
    }
}