namespace SimpleNetworking.Packets
{
    public interface IPacketDataResolver
    {
        T Deserialize<T>(byte[] dataBytes);
        byte[] Serialize<T>(T data);
    }
}