using System.Net;
using System.Net.Sockets;
using SimpleNetworking.Interfaces;
using System;
using SimpleNetworking.Packets.Packets;

namespace SimpleNetworking.Packets
{
    public abstract class AsyncServer
    {
        public bool IsWorking { get; protected set; }
        public ILogger Logger { get; set; }

        private TcpListener _connectionListener;

        private readonly PacketResolver _packetResolver;
        private readonly List<TcpClient> _clients;
        private readonly PacketHandlersContainer _packetHandlersContainer;
        private readonly CancellationTokenSource _stopServerCancellation;

        protected AsyncServer()
        {
            Logger = new ConsoleLogger();
            _packetResolver = new PacketResolver();
            _clients = new List<TcpClient>();
            _packetHandlersContainer = new PacketHandlersContainer();
            _stopServerCancellation = new CancellationTokenSource();
            _stopServerCancellation.Token.Register(() => { });
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

            ListenConnections();
        }

        public void StopServer()
        {
            IsWorking = false;
            OnBeforeServerStopped();
            _connectionListener.Stop();
            _stopServerCancellation.Cancel();
        }

        public void RegisterPacketHandler(PacketHandler packetHandler) => _packetHandlersContainer.Register(nameof(packetHandler), packetHandler);

        protected async Task ListenClient(TcpClient client, CancellationToken cancellationToken)
        {
            NetworkStream clientStream = client.GetStream();

            byte[] buffer = new byte[sizeof(int)];

            try
            {
                while (IsWorking)
                {
                    await clientStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    int packetSize = BitConverter.ToInt32(buffer, 0);

                    byte[] packetBytes = new byte[packetSize];

                    int readBytesCount = await clientStream.ReadAsync(packetBytes, 0, packetBytes.Length, cancellationToken);

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

        private async Task ListenConnections()
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
                    ListenClient(newClient, _stopServerCancellation.Token);
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

        protected virtual void OnClientConnected(TcpClient tcpClient) { }

        protected virtual void OnClientDisconnected(TcpClient tcpClient) { }

        protected virtual void OnReceivedPacket(TcpClient tcpClient, Packet packet)
        {
            if (!_packetHandlersContainer.TryGetHandler(packet.handlerName, out PacketHandler packetHandler)) return;

            packetHandler.OnPacketReceived(packet);
        }

        protected virtual void OnBeforeServerStopped() { }
    }
}