namespace RedGate.Ipc.Channel
{
    internal class ChannelMessage
    {
        public int HandlerCode { get; }
        public byte[] Payload { get; }

        public ChannelMessage(int handlerCode, byte[] payload)
        {
            HandlerCode = handlerCode;
            Payload = payload;
        }
    }
}