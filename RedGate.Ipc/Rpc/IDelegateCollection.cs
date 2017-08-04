using System;
using System.Collections.Generic;

namespace RedGate.Ipc.Rpc
{
    public interface IDelegateCollection
    {
        Dictionary<Type, KeyValuePair<Type, Func<object, object>>> DuplexDelegateFactories { get; }
        List<Func<Type, object>> DependencyInjectors { get; }
        Dictionary<string, Type> TypeAliases { get; }
    }
}