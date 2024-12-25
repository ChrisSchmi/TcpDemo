using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TcpDemo.Client
{
    public class ModernTcpClient
    {
        const int ReadBufferSize = 1024 * 2;
        const int SendTimeout = 5000;
        const int ReceiveTimeout = 5000;
        private IPAddress _usedIpAddress;
        private int _usedPort;
        public enum ConnectionStatus : int
        {
            NeverConnected,
            Connecting,
            Connected,
            Disconnected,
            Error
        }
        public bool Connected => _client != null && _client.Connected;
        public ConnectionStatus Status { get; private set; }
        private TcpClient _client;
        private NetworkStream? _stream;
        private CancellationTokenSource _cancellationTokenSource;
        public event EventHandler<DataReceivedEventArgs> OnDataReceived;
        public event EventHandler<ConnectedEventArgs> OnConnected;
        public event EventHandler OnDisconnected;
        public ModernTcpClient()
        {
            Status = ConnectionStatus.NeverConnected;
            _usedIpAddress = IPAddress.None;
            _usedPort = -1;
            _client = new TcpClient();
            _stream = null;
            _cancellationTokenSource = new CancellationTokenSource();

            OnDataReceived = delegate { };
            OnConnected = delegate { };
            OnDisconnected = delegate { };
        }
        public async Task ConnectAsync(IPAddress ipAddress, int port)
        {
            try
            {
                _usedIpAddress = ipAddress;
                _usedPort = port;

                if(Status == ConnectionStatus.Connected || Status == ConnectionStatus.Connecting)
                {
                    return;
                }

                Status = ConnectionStatus.Connecting;

                _client = new TcpClient();
                _client.NoDelay = true;

                _client.SendTimeout = SendTimeout;
                _client.ReceiveTimeout = ReceiveTimeout;

                await _client.ConnectAsync(_usedIpAddress, _usedPort);
                Status = ConnectionStatus.Connected;

                _stream = _client.GetStream();
                _cancellationTokenSource = new CancellationTokenSource();
                
                _ = StartListening(_cancellationTokenSource.Token);

                OnConnected?.Invoke(this, new ConnectedEventArgs(_usedIpAddress, _usedPort));
            }
            catch (Exception)
            {
                Status = ConnectionStatus.Error;
                throw;
            }
        }
        public async Task SendAsync(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }
        public async Task DisconnectAsync()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                if (_stream != null)
                {
                    await _stream.FlushAsync();
                    _stream.Close();
                }
                _client?.Close();
                Status = ConnectionStatus.Disconnected;
                OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting: {ex.Message}");
            }
        }
        private async Task StartListening(CancellationToken cancellationToken)
        {
            var buffer = new byte[ReadBufferSize];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_stream == null)
                    {
                        OnDisconnected?.Invoke(this, EventArgs.Empty);
                        break;
                    }

                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        OnDisconnected?.Invoke(this, EventArgs.Empty);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnDataReceived?.Invoke(this, new DataReceivedEventArgs(message));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    OnDisconnected?.Invoke(this, EventArgs.Empty);
                    break;
                }
            }
        }
    }
}

