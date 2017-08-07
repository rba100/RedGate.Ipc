using System;

namespace RedGate.Ipc
{
    /// <summary>
    /// Can only be used on void methods.
    /// When this is delclared on a method on interface declaration, calls to proxies of this interface
    /// will return immediately after the request is sent, without waiting for the opertion to complete on the server side.
    /// </summary>
    public class ProxyNonBlockingAttribute : Attribute
    {
    }
}
