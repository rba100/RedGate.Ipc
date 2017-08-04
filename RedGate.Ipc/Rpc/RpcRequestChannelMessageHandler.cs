using System;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc.Rpc
{
    internal class RpcRequestChannelMessageHandler : IChannelMessageHandler
    {
        private readonly IRpcRequestHandler m_RpcRequestHandler;
        private readonly IRpcMessageEncoder m_MessageEncoder;
        private readonly IRpcMessageWriter m_RpcMessageWriter;

        public RpcRequestChannelMessageHandler(
            IRpcRequestHandler rpcRequestHandler,
            IRpcMessageEncoder messageEncoder,
            IRpcMessageWriter rpcMessageWriter)
        {
            m_RpcRequestHandler = rpcRequestHandler;
            m_MessageEncoder = messageEncoder;
            m_RpcMessageWriter = rpcMessageWriter;
        }

        public ChannelMessage Handle(ChannelMessage message)
        {
            if (message.Type() != ChannelMessageType.RpcRequest) return message;
            var request = m_MessageEncoder.ToRequest(message);

            RpcResponse rpcResponse = null;
            RpcException rpcException = null;
            try
            {
                rpcResponse = m_RpcRequestHandler.Handle(request);
            }
            catch (Exception exception)
            {
                rpcException = new RpcException(request.QueryId, exception);
            }
            try
            {
                if (rpcResponse != null) m_RpcMessageWriter.Write(rpcResponse);
                if (rpcException != null) m_RpcMessageWriter.Write(rpcException);
            }
            catch (ChannelFaultedException)
            {
                // Other components will handle disconnection
            }

            return null;
        }
    }
}