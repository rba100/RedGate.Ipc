﻿using System;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IDelegateProvider m_DelegateProvider;

        public ConnectionFactory(IDelegateProvider delegateProvider)
        {
            if (delegateProvider == null) throw new ArgumentNullException(nameof(delegateProvider));

            m_DelegateProvider = delegateProvider;
        }

        public IConnection Create(string connectionId, IChannelStream stream)
        {
            var jsonSerialiser = new TinyJsonSerializer();

            var channelMessageStream = new ChannelMessageStream(stream);
            var channelMessageSerializer = new ChannelMessageSerializer();
            var channelMessageWriter = new ChannelMessageWriter(channelMessageStream, channelMessageSerializer);

            var rpcMessageEncoder = new RpcMessageEncoder(jsonSerialiser);
            var rpcMessageWriter = new RpcMessageWriter(channelMessageWriter, rpcMessageEncoder);
            var rpcRequestHandler = new RpcRequestHandler(m_DelegateProvider, jsonSerialiser);
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
            channelMessageReader.Start();

            return connection;
        }
    }
}