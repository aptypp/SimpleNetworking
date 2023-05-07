using System.Net;
using System.Net.Sockets;
using SimpleNetworking.Logger;
using SimpleNetworking.Packets;

namespace SimpleNetworking
{
    public abstract class AsyncServer
    {
        public bool IsWorking { get; protected set; }
        public ILogger Logger { get; set; }

        protected TcpListener _connectionListener;

        protected readonly PacketResolver _packetResolver;
        protected readonly List<TcpClient> _clients;
        protected readonly PacketHandlersContainer _packetHandlersContainer;
        protected readonly CancellationTokenSource _stopServerCancellationTokenSource;

        protected AsyncServer()
        {
            Logger = new ConsoleLogger();
            _packetResolver = new PacketResolver();
            _clients = new List<TcpClient>();
            _packetHandlersContainer = new PacketHandlersContainer();
            _stopServerCancellationTokenSource = new CancellationTokenSource();
            _stopServerCancellationTokenSource.Token.Register(() => { });
        }

        ~AsyncServer()
        {
            if (IsWorking) StopServer();
        }

        public void StartServer(string address, int port)
        {
            IPAddress ipAddress = IPAddress.Parse(address);
            _connectionListener = new TcpListener(ipAddress, port);

            Logger.Log($"Server started at {ipAddress}:{port}");

            IsWorking = true;

            ListenConnectionsAsync();
        }

        public void StopServer()
        {
            IsWorking = false;
            OnBeforeServerStopped();
            _connectionListener.Stop();
            _stopServerCancellationTokenSource.Cancel();
        }

        public void RegisterPacketHandler(PacketHandler packetHandler) => _packetHandlersContainer.Register(nameof(packetHandler), packetHandler);

        public async Task SendPacketAsync(Packet packet, TcpClient tcpClient) =>
            await tcpClient.GetStream().WriteAsync(_packetResolver.Serialize(packet));

        protected async Task ListenClientAsync(TcpClient client, CancellationTokenSource cancellationTokenSource)
        {
            NetworkStream clientStream = client.GetStream();

            byte[] buffer = new byte[sizeof(int)];

            try
            {
                while (IsWorking)
                {
                    await clientStream.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);

                    int packetSize = BitConverter.ToInt32(buffer, 0);

                    byte[] packetBytes = new byte[packetSize];

                    int readBytesCount = await clientStream.ReadAsync(packetBytes, 0, packetBytes.Length, cancellationTokenSource.Token);

                    if (readBytesCount != packetSize)
                    {
                        Logger.Log("Incorrect packet read");
                        continue;
                    }

                    Packet packet = _packetResolver.Deserialize(packetBytes);

                    OnReceivedPacket(client, packet);
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.Message);
            }
            finally
            {
                if (IsWorking)
                {
                    _clients.Remove(client);
                    OnClientDisconnected(client);
                }
            }
        }

        private async Task ListenConnectionsAsync()
        {
            _connectionListener.Start();

            try
            {
                while (IsWorking)
                {
                    TcpClient newClient = await _connectionListener.AcceptTcpClientAsync();
                    IPEndPoint clientIpEndPoint = (IPEndPoint)newClient.Client.RemoteEndPoint;
                    Logger.Log($"New client connected {clientIpEndPoint.Address}:{clientIpEndPoint.Port}");
                    OnClientConnected(newClient);
                    ListenClientAsync(newClient, _stopServerCancellationTokenSource);
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.Message);
            }
            finally
            {
                StopServer();
            }
        }

        protected abstract void OnClientConnected(TcpClient tcpClient);

        protected abstract void OnClientDisconnected(TcpClient tcpClient);

        protected virtual void OnReceivedPacket(TcpClient tcpClient, Packet packet)
        {
            if (!_packetHandlersContainer.TryGetHandler(packet.handlerName, out PacketHandler packetHandler)) return;

            packetHandler.OnPacketReceived(packet);
        }

        protected abstract void OnBeforeServerStopped();
    }
}