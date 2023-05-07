using System.Net;
using System.Net.Sockets;
using SimpleNetworking.Packets;

namespace SimpleNetworking
{
    public abstract class AsyncClient
    {
        protected bool _isConnected;

        protected readonly TcpClient _tcpClient;
        protected readonly PacketResolver _packetResolver;
        protected readonly PacketHandlersContainer _packetHandlersContainer;
        protected readonly CancellationTokenSource _disconnectCientCancellationTokenSource;

        protected AsyncClient()
        {
            _tcpClient = new TcpClient();
            _packetResolver = new PacketResolver();
            _packetHandlersContainer = new PacketHandlersContainer();
            _disconnectCientCancellationTokenSource = new CancellationTokenSource();
            _disconnectCientCancellationTokenSource.Token.Register(() => { });
        }

        public void RegisterPacketHandler(PacketHandler packetHandler)
        {
            _packetHandlersContainer.Register(nameof(packetHandler), packetHandler);
        }

        public async Task ConnectToServerAsync(string address, int port)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(address);

                await _tcpClient.ConnectAsync(ipAddress, port);

                _isConnected = true;

                OnConnected();
            }
            catch (Exception e) { }
        }

        public void DisconnectFromServer()
        {
            OnBeforeDisconnected();
            _tcpClient.Close();
        }

        public async Task SendPacketAsync(Packet packet) => await _tcpClient.GetStream().WriteAsync(_packetResolver.Serialize(packet));

        protected async Task ListenServerAsync(CancellationTokenSource cancellationTokenSource)
        {
            NetworkStream clientStream = _tcpClient.GetStream();

            byte[] buffer = new byte[sizeof(int)];

            try
            {
                while (_isConnected)
                {
                    await clientStream.ReadAsync(buffer, 0, sizeof(int), cancellationTokenSource.Token);

                    int packetSize = BitConverter.ToInt32(buffer, 0);

                    byte[] packetBytes = new byte[packetSize];

                    int readBytesCount = await clientStream.ReadAsync(packetBytes, 0, packetBytes.Length, cancellationTokenSource.Token);

                    if (readBytesCount != packetSize) continue;

                    Packet packet = _packetResolver.Deserialize(packetBytes);

                    OnReceivedPacket(packet);
                }
            }
            catch (Exception e) { }
        }

        protected abstract void OnConnected();
        protected abstract void OnBeforeDisconnected();
        protected abstract void OnReceivedPacket(Packet packet);
    }
}