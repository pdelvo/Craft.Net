using Craft.Net.Data;
namespace Craft.Net.Server.Packets
{
    public class CloseWindowPacket : Packet
    {
        public byte WindowId;

        public override byte PacketId
        {
            get { return 0x65; }
        }

        public override int TryReadPacket(byte[] buffer, int length)
        {
            int offset = 1;
            if (!DataUtility.TryReadByte(buffer, ref offset, out WindowId))
                return -1;
            return offset;
        }

        public override void HandlePacket(MinecraftServer server, MinecraftClient client)
        {
            // Do nothing
            // TODO: Do something?
        }

        public override void SendPacket(MinecraftServer server, MinecraftClient client)
        {
            var buffer = new[] {PacketId, WindowId};
            client.SendData(buffer);
        }
    }
}