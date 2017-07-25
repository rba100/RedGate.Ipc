using System;
using System.IO;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IRpcRequestHandler m_RpcRequestHandler;

        public ConnectionFactory(IRpcRequestHandler rpcRequestHandler)
        {
            if (rpcRequestHandler == null) throw new ArgumentNullException(nameof(rpcRequestHandler));

            m_RpcRequestHandler = rpcRequestHandler;
        }

        public IConnection Create(string connectionId, IChannelStream stream)
        {
            var jsonSerialiser = new TinyJsonSerializer();
            var rpcMessageEncoder = new RpcMessageEncoder(jsonSerialiser);
            var messageStream = new ChannelMessageStream(stream);
            var channelMessageSerializer = new ChannelMessageSerializer();
            var channelWriter = new ChannelMessageWriter(messageStream, channelMessageSerializer);
            
            var rpcMessageWriter = new RpcMessageWriter(channelWriter, rpcMessageEncoder);
            var messageBroker = new RpcMessageBroker(rpcMessageWriter, m_RpcRequestHandler);
            var rpcMessageHandler = new RpcChannelMessageHandler(messageBroker, rpcMessageEncoder);
            var pipeline = new ChannelMessagePipeline(new []{ rpcMessageHandler });
            var channelMessageDispatcher = new ChannelMessageDispatcher(messageStream, channelMessageSerializer, pipeline);

            var connection = new Connection(connectionId, messageBroker, channelMessageDispatcher);

            channelMessageDispatcher.Start();
            return connection;
        }
    }
}