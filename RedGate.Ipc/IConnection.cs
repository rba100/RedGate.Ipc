using System;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public interface IConnection : IDisposable
    {
        string ConnectionId { get; }
        IRpcMessageBroker RpcMessageBroker { get; }

        bool IsConnected { get; }

        event ClientDisconnectedEventHandler Disconnected;
    }
}