using System;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc.Rpc
{
    internal class RpcMessageWriter : IRpcMessageWriter
    {
        private readonly IChannelMessageWriter m_ChannelMessageWriter;
        private readonly IRpcMessageEncoder m_MessageEncoder;

        public RpcMessageWriter(
            IChannelMessageWriter channelMessageWriter,
            IRpcMessageEncoder messageEncoder)
        {
            if (channelMessageWriter == null) throw new ArgumentNullException(nameof(channelMessageWriter));
            if (messageEncoder == null) throw new ArgumentNullException(nameof(messageEncoder));

            m_ChannelMessageWriter = channelMessageWriter;
            m_MessageEncoder = messageEncoder;
        }

        public void Write(RpcRequest request)
        {
            m_ChannelMessageWriter.Write(m_MessageEncoder.ToChannelMessage(request));
        }

        public void Write(RpcResponse response)
        {
            m_ChannelMessageWriter.Write(m_MessageEncoder.ToChannelMessage(response));
        }

        public void Write(RpcException exception)
        {
            m_ChannelMessageWriter.Write(m_MessageEncoder.ToChannelMessage(exception));
        }
    }
}
