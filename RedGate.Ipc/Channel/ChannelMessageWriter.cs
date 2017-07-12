namespace RedGate.Ipc.Channel
{
    internal class ChannelMessageWriter : IChannelMessageWriter
    {
        private readonly IMessageStream m_MessageStream;
        private readonly IChannelMessageSerializer m_ChannelMessageSerializer;

        public ChannelMessageWriter(IMessageStream messageStream, IChannelMessageSerializer channelMessageSerializer)
        {
            m_MessageStream = messageStream;
            m_ChannelMessageSerializer = channelMessageSerializer;
        }

        public void Write(ChannelMessage channelMessage)
        {
            try
            {
                m_MessageStream.Write(m_ChannelMessageSerializer.ToBytes(channelMessage));
            }
            catch
            {
                throw new ChannelFaultedException();
            }
        }
    }
}