using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.Channel
{
    internal class ChannelMessageWriter : IChannelMessageWriter
    {
        private readonly IChannelMessageStream m_ChannelMessageStream;
        private readonly IChannelMessageSerializer m_ChannelMessageSerializer;

        public ChannelMessageWriter(IChannelMessageStream channelMessageStream, IChannelMessageSerializer channelMessageSerializer)
        {
            m_ChannelMessageStream = channelMessageStream;
            m_ChannelMessageSerializer = channelMessageSerializer;
        }

        public void Write(ChannelMessage channelMessage)
        {
            try
            {
                m_ChannelMessageStream.Write(m_ChannelMessageSerializer.ToBytes(channelMessage));
            }
            catch(ChannelFaultedException e)
            {
                throw new ChannelFaultedException("The connection was closed before the message was written. " + e.Message);
            }
        }
    }
}