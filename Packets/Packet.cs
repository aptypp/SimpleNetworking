namespace SimpleNetworking.Packets
{
    public sealed class Packet
    {
        public int dataLength;
        public int handlerNameLength;
        public byte[] data;
        public string handlerName;
    }
}