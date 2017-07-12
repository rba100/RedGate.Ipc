using RedGate.Ipc.Channel;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    internal class Connection : IConnection
    {
        private readonly IChannelMessageDispatcher m_ChannelMessageDispatcher;
        public string ConnectionId { get; }
        public IRpcMessageBroker RpcMessageBroker { get; }

        public Connection(
            string connectionId,
            IRpcMessageBroker rpcMessageBroker,
            IChannelMessageDispatcher channelMessageDispatcher)
        {
            m_ChannelMessageDispatcher = channelMessageDispatcher;
            ConnectionId = connectionId;
            RpcMessageBroker = rpcMessageBroker;
            channelMessageDispatcher.Disconnected += OnDisconnected;
            rpcMessageBroker.Disconnected += OnDisconnected;
        }

        private void OnDisconnected()
        {
            IsConnected = false;
            Disconnected?.Invoke(new DisconnectedEventArgs(this));
        }

        public event ClientDisconnectedEventHandler Disconnected;

        public void Dispose()
        {
            IsConnected = false;
            m_ChannelMessageDispatcher?.Stop();
            RpcMessageBroker.Dispose();
        }

        public bool IsConnected { get; private set; } = true;
    }
}