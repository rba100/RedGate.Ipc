using System;
using System.Collections.Generic;

using RedGate.Ipc.Channel;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    internal class Connection : IConnection
    {
        private readonly List<IDisposable> m_Disposables;

        public bool IsConnected { get; private set; }

        public string ConnectionId { get; }
        
        public event ClientDisconnectedEventHandler Disconnected = delegate { };

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
            IsConnected = true;
            rpcMessageBroker.Disconnected += OnDisconnected;
            channelMessageReader.Disconnected += OnDisconnected;
        }

        private void OnDisconnected()
        {
            if (IsConnected)
            {
                // Race condition of multiple OnDisconnected cos dispose sets IsConnected
                Dispose();
            }
        }

        public void Dispose()
        {
            IsConnected = false;
            Disconnected.Invoke(new DisconnectedEventArgs(this));
            m_Disposables.ForEach(d => d.Dispose());
        }
    }
}