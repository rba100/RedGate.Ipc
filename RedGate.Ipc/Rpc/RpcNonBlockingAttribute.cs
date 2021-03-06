﻿using RedGate.Ipc.Proxy;

namespace RedGate.Ipc.Rpc
{
    /// <summary>
    /// Must only be used on void methods.
    /// When delclared on a void method on an interface declaration, calls to that method on a proxy
    /// will return immediately without waiting for the server to complete the operation.
    /// </summary>
    public class RpcNonBlockingAttribute : ProxyShouldImplementAttribute
    {
    }
}
