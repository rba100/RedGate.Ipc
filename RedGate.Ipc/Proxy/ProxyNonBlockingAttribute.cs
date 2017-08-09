using System;

namespace RedGate.Ipc.Proxy
{
    /// <summary>
    /// Must only be used on void methods.
    /// When delclared on a void method on an interface declaration, calls to that method on a dynamic proxy
    /// will return immediately without waiting for the server to complete the operation.
    /// </summary>
    public class ProxyNonBlockingAttribute : Attribute
    {
    }
}