using System;

namespace RedGate.Ipc
{
    public interface IReliableConnectionAgent : IDisposable
    {
        IConnection TryGetConnection(int timeoutMs);
    }
}