using System;

namespace RedGate.Ipc.Rpc
{
    public interface IRpcMessageBroker : IDisposable
    {
        RpcResponse Send(RpcRequest request);
        void HandleInbound(RpcRequest message);
        void HandleInbound(RpcResponse message);
        void HandleInbound(RpcException message);

        event DisconnectedEventHandler Disconnected;
    }
}