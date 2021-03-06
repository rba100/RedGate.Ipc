﻿using System;

namespace RedGate.Ipc.Rpc
{
    public interface IRpcMessageBroker : IDisposable
    {
        RpcResponse Send(RpcRequest request);
        void HandleInbound(RpcResponse message);
        void HandleInbound(RpcException message);
        void BeginRequest(RpcRequest request, RequestToken requestToken);
    }
}