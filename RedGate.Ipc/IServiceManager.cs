using System;
using System.IO;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public interface IServiceManager
    {
        void Register<T>(object implementation);
        void AddEndpoint(IEndpoint endpoint);
        void Start();
        void Stop();

        event ClientConnectedEventHandler ClientConnected;
    }

    public interface IEndpoint
    {
        event ChannelConnectedEventHandler ChannelConnected;
        void Start();
        void Stop();
    }

    public delegate void ChannelConnectedEventHandler(ChannelConnectedEventArgs args);
    public delegate void ChannelDisconnectedEventHandler(ChannelDisconnectedEventArgs args);
    public delegate void ClientConnectedEventHandler(ClientConnectedEventArgs args);
    public delegate void ClientDisconnectedEventHandler(ClientDisconnectedEventArgs args);

    public sealed class ChannelConnectedEventArgs
    {
        public string ConnectionId { get; }
        public Stream ClientStream { get; }

        public ChannelConnectedEventArgs(string connectionId, Stream clientStream)
        {
            ConnectionId = connectionId;
            ClientStream = clientStream;
        }
    }

    public sealed class ClientConnectedEventArgs
    {
        public IConnection Connection { get; }

        public ClientConnectedEventArgs(IConnection connection)
        {
            Connection = connection;
        }
    }

    public sealed class ClientDisconnectedEventArgs
    {
        public IConnection Connection { get; }

        public ClientDisconnectedEventArgs(IConnection connection)
        {
            Connection = connection;
        }
    }

    public interface IConnection : IDisposable
    {
        string ConnectionId { get; }
        IRpcMessageBroker RpcMessageBroker { get; }

        event ClientDisconnectedEventHandler Disconnected;
    }

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
            channelMessageDispatcher.Disconnected += () => Disconnected?.Invoke(new ClientDisconnectedEventArgs(this));
            rpcMessageBroker.Disconnected += () => Disconnected?.Invoke(new ClientDisconnectedEventArgs(this));
        }

        public event ClientDisconnectedEventHandler Disconnected;

        public void Dispose()
        {
            m_ChannelMessageDispatcher?.Stop();
            RpcMessageBroker.Dispose();
        }
    }

    public sealed class ChannelDisconnectedEventArgs
    {
        public string ConnectionId { get; set; }
    }
}
