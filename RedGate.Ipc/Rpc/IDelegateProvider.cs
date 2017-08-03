using System;

namespace RedGate.Ipc.Rpc
{
    public interface IDelegateProvider : IDelegateRegistrar
    {
        object Get(string typeFullName);
        object Get(Type type);
        T Get<T>();
    }
}
