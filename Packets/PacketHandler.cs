namespace SimpleNetworking.Packets
{
    public abstract class PacketHandler
    {
        private IPacketDataResolver _packetDataResolver;

        protected PacketHandler(IPacketDataResolver packetDataResolver)
        {
            _packetDataResolver = packetDataResolver;
        }

        public abstract void OnPacketReceived(Packet packet);
    }
}