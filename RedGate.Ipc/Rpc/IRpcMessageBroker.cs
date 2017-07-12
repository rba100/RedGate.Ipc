﻿using System;

namespace RedGate.Ipc.Rpc
{
    public interface IRpcMessageBroker : IDisposable
    {
        RpcResponse Send(RpcRequest request);
        void HandleInbound(RpcRequest message);
        void HandleInbound(RpcResponse message);

        event DisconnectedEventHandler Disconnected;
    }
}