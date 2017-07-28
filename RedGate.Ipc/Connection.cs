using System;
using System.Collections.Generic;

using RedGate.Ipc.Channel;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    internal class Connection : IConnection
    {
        private readonly List<IDisposable> m_Disposables;

        private volatile bool m_Disposed;
        public bool IsConnected => !m_Disposed;
        private readonly object m_IsDisposedLock = new object();

        public string ConnectionId { get; }

        private event ClientDisconnectedEventHandler DisconnectedImpl = delegate { };
        public event ClientDisconnectedEventHandler Disconnected
        {

            add
            {
                DisconnectedImpl += value;
                lock (m_IsDisposedLock)
                {
                    if (m_Disposed)
                    {
                        value(new DisconnectedEventArgs(this));
                    }
                }
            }

            remove
            {
                DisconnectedImpl -= value;
            }
        }

        public IRpcMessageBroker RpcMessageBroker { get; }

        internal Connection(
            string connectionId,
            IRpcMessageBroker rpcMessageBroker,
            IEnumerable<IDisconnectReporter> disconnectReporters,
            IEnumerable<IDisposable> disposeChain)
        {
            m_Disposables = new List<IDisposable>(disposeChain);
            ConnectionId = connectionId;
            RpcMessageBroker = rpcMessageBroker;
            foreach (var reporter in disconnectReporters) reporter.Disconnected += OnDisconnected;
        }

        private void OnDisconnected()
        {
            if (IsConnected)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            bool disposed;
            lock (m_IsDisposedLock)
            {
                disposed = m_Disposed;
                m_Disposed = true;
            }
            if (!disposed)
            {
                foreach (var d in m_Disposables) d.Dispose();
                try
                {
                    DisconnectedImpl.Invoke(new DisconnectedEventArgs(this));
                }
                catch
                {
                    // Dispose must not throw
                }
            }
        }
    }
}