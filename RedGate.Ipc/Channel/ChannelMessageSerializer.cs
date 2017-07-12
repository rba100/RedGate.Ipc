using System;

namespace RedGate.Ipc.Channel
{
    internal class ChannelMessageSerializer : IChannelMessageSerializer
    {
        public ChannelMessage FromBytes(byte[] bytes)
        {
            if (bytes.Length < 4) throw new Exception("Malformed channel message");
            var header = BitConverter.ToInt32(bytes, 0);
            var payload = new byte[bytes.Length - 4];
            Array.Copy(bytes, 4, payload, 0, payload.Length);
            return new ChannelMessage(header, payload);
        }

        public byte[] ToBytes(ChannelMessage channelMessage)
        {
            var serialized = new byte[channelMessage.Payload.Length + 4];
            var header = BitConverter.GetBytes(channelMessage.HandlerCode);
            Array.Copy(header, 0, serialized, 0, 4);
            Array.Copy(channelMessage.Payload, 0, serialized, 4, channelMessage.Payload.Length);
            return serialized;
        }
    }
}