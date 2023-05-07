namespace SimpleNetworking.Packets.Packets
{
    public class PacketHandlersContainer
    {
        private readonly Dictionary<string, PacketHandler> _handlers;

        public PacketHandlersContainer() => _handlers = new Dictionary<string, PacketHandler>();

        public void Register(string handlerName, PacketHandler packetHandler) => _handlers.Add(handlerName, packetHandler);

        public void Remove(string handlerName) => _handlers.Remove(handlerName);

        public bool TryGetHandler(string handlerName, out PacketHandler packetHandler) => _handlers.TryGetValue(handlerName, out packetHandler);
    }
}