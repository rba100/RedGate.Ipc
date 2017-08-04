using System;

namespace RedGate.Ipc.Rpc
{
    public interface IDelegateProvider
    {
        object Get(string typeFullName);
        object Get(Type type);
        T Get<T>();
    }
}
