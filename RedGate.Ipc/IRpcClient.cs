using System;

namespace RedGate.Ipc
{
    public interface IRpcClient : IDisposable
    {
        void Register<T>(object implementation);

        T CreateProxy<T>();
    }
}