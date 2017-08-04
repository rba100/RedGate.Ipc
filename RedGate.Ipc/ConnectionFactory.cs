using System;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IDelegateCollection m_DelegateCollection;

        public ConnectionFactory(IDelegateCollection delegateCollection)
        {
            m_DelegateCollection = delegateCollection;
        }

        public IConnection Create(string connectionId, IChannelStream stream)
        {
            var jsonSerialiser = new TinyJsonSerializer();

            var channelMessageStream = new ChannelMessageStream(stream);
            var channelMessageSerializer = new ChannelMessageSerializer();
            var channelMessageWriter = new ChannelMessageWriter(channelMessageStream, channelMessageSerializer);

            var rpcMessageEncoder = new RpcMessageEncoder(jsonSerialiser);
            var rpcMessageWriter = new RpcMessageWriter(channelMessageWriter, rpcMessageEncoder);
            var rpcMessageBroker = new RpcMessageBroker(rpcMessageWriter);
            var delegateProvider = new DuplexDelegateProvider(m_DelegateCollection, rpcMessageBroker);
            var rpcRequestHandler = new RpcRequestHandler(delegateProvider, jsonSerialiser);
            
            var rpcResponseMessageHandler = new RpcResponseChannelMessageHandler(rpcMessageBroker, rpcMessageEncoder);
            var rpcRequestMessageHandler = new RpcRequestChannelMessageHandler(rpcRequestHandler, rpcMessageEncoder, rpcMessageWriter);
            var pipeline = new ChannelMessagePipeline(new IChannelMessageHandler[] { rpcResponseMessageHandler, rpcRequestMessageHandler });
            var channelMessageReader = new ChannelMessageReader(channelMessageStream, channelMessageSerializer, pipeline);

            var connection = new Connection(
                connectionId,
                rpcMessageBroker,
                disconnectReporters: new IDisconnectReporter[] { stream },
                disposeChain: new IDisposable[] { channelMessageReader, rpcMessageBroker });

            rpcRequestHandler.OwningConnection = connection;
            channelMessageReader.Start();

            return connection;
        }
    }
}