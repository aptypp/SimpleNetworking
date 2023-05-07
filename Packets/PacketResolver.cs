using System.Text;

namespace SimpleNetworking.Packets
{
    public class PacketResolver
    {
        public byte[] Serialize(Packet packet)
        {
            byte[] handlerNameLength = BitConverter.GetBytes(packet.handlerNameLength);
            byte[] handlerName = Encoding.Unicode.GetBytes(packet.handlerName);
            byte[] packetDataLength = BitConverter.GetBytes(packet.dataLength);
            byte[] packetData = packet.data;
            byte[] packetLength = BitConverter.GetBytes(handlerNameLength.Length + handlerName.Length + packetDataLength.Length + packetData.Length);

            List<byte> dataList = new List<byte>();

            dataList.AddRange(packetLength);
            dataList.AddRange(handlerNameLength);
            dataList.AddRange(handlerName);
            dataList.AddRange(packetDataLength);
            dataList.AddRange(packetData);

            return dataList.ToArray();
        }

        public Packet Deserialize(byte[] packetBytes)
        {
            Packet packet = new Packet();

            packet.handlerNameLength = BitConverter.ToInt32(packetBytes, sizeof(int));
            packet.dataLength = BitConverter.ToInt32(packetBytes, sizeof(int) + packet.handlerNameLength);
            packet.handlerName = Encoding.Unicode.GetString(packetBytes, sizeof(int), packet.handlerNameLength);
            packet.data = new byte[packet.dataLength];
            Array.ConstrainedCopy(packetBytes, sizeof(int) + packet.handlerNameLength, packet.data, 0, packet.dataLength);

            return packet;
        }
    }
}