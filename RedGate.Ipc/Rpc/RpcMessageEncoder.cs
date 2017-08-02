using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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

        public ChannelMessage ToChannelMessage(RpcException exception)
        {
            using (var memStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memStream, exception.Exception);
                var binding = new RpcExceptionBinding
                {
                    QueryId = exception.QueryId,
                    Exception = Convert.ToBase64String(memStream.ToArray())
                };

                var payload = Encoding.UTF8.GetBytes(
                m_JsonSerializer.Serialize(binding));

                return new ChannelMessage((int)ChannelMessageType.RpcException, payload);
            }
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

        public RpcException ToException(ChannelMessage channelMessage)
        {
            if (!channelMessage.IsMessageType(ChannelMessageType.RpcException))
                throw new InvalidOperationException("ChannelMessage is not an RpcException");

            var json = Encoding.UTF8.GetString(channelMessage.Payload);
            return FromBinding(m_JsonSerializer.Deserialize<RpcExceptionBinding>(json));
        }

        private static RpcRequest FromBinding(RpcRequestBinding binding)
        {
            return new RpcRequest(
                binding.QueryId,
                binding.Interface,
                binding.MethodSignature,
                binding.Arguments);
        }

        private static RpcResponse FromBinding(RpcResponseBinding binding)
        {
            return new RpcResponse(
                binding.QueryId,
                binding.ReturnValue);
        }

        private static RpcException FromBinding(RpcExceptionBinding binding)
        {
            var formatter = new BinaryFormatter();
            try
            {
                using (var memStream = new MemoryStream(Convert.FromBase64String(binding.Exception)))
                {
                    return new RpcException(
                        binding.QueryId,
                        (Exception) formatter.Deserialize(memStream));
                }
            }
            catch (Exception)
            {
                return new RpcException(binding.QueryId, new Exception("The operation could not be completed."));
                // TODO: Log this as error
            }
        }
    }
}