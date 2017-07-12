using System;
using System.Text;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Json;

namespace RedGate.Ipc.Rpc
{
    internal class RpcMessageEncoder : IRpcMessageEncoder
    {
        private readonly IJsonSerializer m_JsonSerializer;

        public RpcMessageEncoder(IJsonSerializer jsonSerializer)
        {
            m_JsonSerializer = jsonSerializer;
        }

        public ChannelMessage ToChannelMessage(RpcResponse response)
        {
            var payload = Encoding.UTF8.GetBytes(
                m_JsonSerializer.Serialize(response));
            return new ChannelMessage((int) ChannelMessageType.RpcResponse, payload);
        }

        public ChannelMessage ToChannelMessage(RpcRequest request)
        {
            var payload = Encoding.UTF8.GetBytes(
                m_JsonSerializer.Serialize(request));
            return new ChannelMessage((int)ChannelMessageType.RpcRequest, payload);
        }

        public RpcResponse ToResponse(ChannelMessage channelMessage)
        {
            if(!channelMessage.IsMessageType(ChannelMessageType.RpcResponse))
                throw new InvalidOperationException("ChannelMessage is not an RpcResponse");

            var json = Encoding.UTF8.GetString(channelMessage.Payload);
            return FromBinding(m_JsonSerializer.Deserialize<RpcResponseBinding>(json));
        }

        public RpcRequest ToRequest(ChannelMessage channelMessage)
        {
            if (!channelMessage.IsMessageType(ChannelMessageType.RpcRequest))
                throw new InvalidOperationException("ChannelMessage is not an RpcRequest");

            var json = Encoding.UTF8.GetString(channelMessage.Payload);
            return FromBinding(m_JsonSerializer.Deserialize<RpcRequestBinding>(json));
        }

        private static RpcRequest FromBinding(RpcRequestBinding binding)
        {
            return new RpcRequest(
                binding.QueryId,
                binding.Interface,
                binding.Method,
                binding.Arguments);
        }

        private static RpcResponse FromBinding(RpcResponseBinding binding)
        {
            return new RpcResponse(
                binding.QueryId,
                binding.ReturnValue);
        }
    }
}