using System;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IDelegateResolver m_DelegateResolver;

        public ConnectionFactory(IDelegateResolver delegateResolver)
        {
            if (delegateResolver == null) throw new ArgumentNullException(nameof(delegateResolver));

            m_DelegateResolver = delegateResolver;
        }

        public IConnection Create(string connectionId, IChannelStream stream)
        {
            var jsonSerialiser = new TinyJsonSerializer();

            var channelMessageStream = new ChannelMessageStream(stream);
            var channelMessageSerializer = new ChannelMessageSerializer();
            var channelMessageWriter = new ChannelMessageWriter(channelMessageStream, channelMessageSerializer);

            var rpcMessageEncoder = new RpcMessageEncoder(jsonSerialiser);
            var rpcMessageWriter = new RpcMessageWriter(channelMessageWriter, rpcMessageEncoder);
            var rpcRequestHandler = new RpcRequestHandler(m_DelegateResolver, jsonSerialiser);
            var rpcMessageBroker = new RpcMessageBroker(rpcMessageWriter, rpcRequestHandler);
            var rpcMessageHandler = new RpcChannelMessageHandler(rpcMessageBroker, rpcMessageEncoder);

            var pipeline = new ChannelMessagePipeline(new[] { rpcMessageHandler });
            var channelMessageReader = new ChannelMessageReader(channelMessageStream, channelMessageSerializer, pipeline);

            var connection = new Connection(
                connectionId,
                rpcMessageBroker,
                disconnectReporters: new IDisconnectReporter[] { stream },
                disposeChain: new IDisposable[] { channelMessageReader, rpcMessageBroker });

            rpcRequestHandler.OwningConnection = connection;
            return connection;
        }
    }
}