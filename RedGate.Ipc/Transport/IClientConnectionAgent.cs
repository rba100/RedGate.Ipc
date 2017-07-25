using System;

namespace RedGate.Ipc
{
    public interface IClientConnectionAgent : IDisposable
    {
        IConnection TryGetConnection(int timeoutMs);
    }
}