using System;
using System.Collections.Generic;

using RedGate.Ipc.Channel;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    internal class Connection : IConnection
    {
        private readonly List<IDisposable> m_Disposables;

        public string ConnectionId { get; }
        public IRpcMessageBroker RpcMessageBroker { get; }

        internal Connection(
            string connectionId, 
            IRpcMessageBroker rpcMessageBroker, 
            ChannelMessageReader channelMessageReader,
            IEnumerable<IDisposable> disposeChain)
        {
            m_Disposables = new List<IDisposable>(disposeChain);
            ConnectionId = connectionId;
            RpcMessageBroker = rpcMessageBroker;
            rpcMessageBroker.Disconnected += OnDisconnected;
            channelMessageReader.Disconnected += OnDisconnected;
        }

        private void OnDisconnected()
        {
            IsConnected = false;
            Disconnected.Invoke(new DisconnectedEventArgs(this));
        }

        public event ClientDisconnectedEventHandler Disconnected = delegate { };

        public void Dispose()
        {
            IsConnected = false;
            m_Disposables.ForEach(d => d.Dispose());
        }

        public bool IsConnected { get; private set; } = true;
    }
}