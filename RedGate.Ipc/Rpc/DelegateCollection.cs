using System;
using System.Collections.Generic;

namespace RedGate.Ipc.Rpc
{
    public class DelegateCollection : IDelegateCollection
    {
        public Dictionary<Type, KeyValuePair<Type, Func<object, object>>> DuplexDelegateFactories { get; }
        public List<Func<Type, object>> DependencyInjectors { get; }
        public Dictionary<string, Type> TypeAliases { get; }

        public DelegateCollection(
            Dictionary<Type, KeyValuePair<Type, Func<object, object>>> duplexDelegateFactories,
            List<Func<Type, object>> dependencyInjectors,
            Dictionary<string, Type> typeAliases)
        {
            DuplexDelegateFactories = duplexDelegateFactories;
            DependencyInjectors = dependencyInjectors;
            TypeAliases = typeAliases;
        }

        public DelegateCollection()
        {
            DuplexDelegateFactories = new Dictionary<Type, KeyValuePair<Type, Func<object, object>>>();
            DependencyInjectors = new List<Func<Type, object>>();
            TypeAliases = new Dictionary<string, Type>();
        }
    }
}