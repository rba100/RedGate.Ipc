using System;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc.Rpc
{
    internal class RpcChannelMessageHandler : IChannelMessageHandler
    {
        private readonly IRpcMessageBroker m_RpcMessageBroker;
        private readonly IRpcMessageEncoder m_MessageEncoder;

        public RpcChannelMessageHandler(
            IRpcMessageBroker rpcMessageBroker, 
            IRpcMessageEncoder messageEncoder)
        {
            if (rpcMessageBroker == null) throw new ArgumentNullException(nameof(rpcMessageBroker));
            if (messageEncoder == null) throw new ArgumentNullException(nameof(messageEncoder));

            m_RpcMessageBroker = rpcMessageBroker;
            m_MessageEncoder = messageEncoder;
        }

        public ChannelMessage Handle(ChannelMessage message)
        {
            switch (message.Type())
            {
                case ChannelMessageType.RpcRequest:
                    var request = m_MessageEncoder.ToRequest(message);
                    m_RpcMessageBroker.HandleInbound(request);
                    return null; // Handled
                case ChannelMessageType.RpcResponse:
                    var response = m_MessageEncoder.ToResponse(message);
                    m_RpcMessageBroker.HandleInbound(response);
                    return null; // Handled
                default:
                    return message;  // Not handled
            }
        }
    }
}
