using System;
using System.Collections.Generic;
using System.Linq;

namespace RedGate.Ipc.Rpc
{
    public class DuplexDelegateProvider : IDelegateProvider
    {
        private readonly IDelegateCollection m_DelegateCollection;
        private readonly IRpcMessageBroker m_RpcMessageBroker;
        private readonly Dictionary<Type, object> m_DelegateCache = new Dictionary<Type, object>();

        public DuplexDelegateProvider(IDelegateCollection delegateCollection, IRpcMessageBroker rpcMessageBroker)
        {
            if (delegateCollection == null) throw new ArgumentNullException(nameof(delegateCollection));
            if (rpcMessageBroker == null) throw new ArgumentNullException(nameof(rpcMessageBroker));

            m_DelegateCollection = delegateCollection;
            m_RpcMessageBroker = rpcMessageBroker;
        }

        public object Get(string typeFullName)
        {
            var type = m_DelegateCollection
                .TypeAliases
                .Where(kvp => typeFullName.StartsWith(kvp.Key)).Select(kvp => kvp.Value).FirstOrDefault();
            type = type ?? Type.GetType(typeFullName);
            return Get(type);
        }

        public object Get(Type type)
        {
            object delegateObject = null;

            if (m_DelegateCache.ContainsKey(type))
            {
                return m_DelegateCache[type];
            }

            if (m_DelegateCollection.DuplexDelegateFactories.ContainsKey(type))
            {
                var callbackContractAndFactory = m_DelegateCollection.DuplexDelegateFactories[type];
                var callbackType = callbackContractAndFactory.Key;
                var delegateFactory = callbackContractAndFactory.Value;
                var rpcClient = new SingleConnectionRpcClient(m_RpcMessageBroker);
                var callback = rpcClient.CreateProxy(callbackType);
                delegateObject = delegateFactory(callback);
            }
            else
            {
                foreach (var di in m_DelegateCollection.DependencyInjectors)
                {
                    delegateObject = di(type);
                    if (delegateObject != null) break;
                }
            }

            if (delegateObject != null) m_DelegateCache.Add(type, delegateObject);

            return delegateObject;
        }

        public T Get<T>()
        {
            return (T)Get(typeof(T));
        }
    }

    public class DelegateProvider : IDelegateProvider
    {
        private readonly IDelegateCollection m_DelegateCollection;
        private readonly Dictionary<Type, object> m_DelegateCache = new Dictionary<Type, object>();

        public DelegateProvider(IDelegateCollection delegateCollection)
        {
            if (delegateCollection == null) throw new ArgumentNullException(nameof(delegateCollection));

            m_DelegateCollection = delegateCollection;
        }

        public object Get(string typeFullName)
        {
            var type = m_DelegateCollection
                .TypeAliases
                .Where(kvp => typeFullName.StartsWith(kvp.Key)).Select(kvp => kvp.Value).FirstOrDefault();
            type = type ?? Type.GetType(typeFullName);
            return Get(type);
        }

        public object Get(Type type)
        {
            if (m_DelegateCache.ContainsKey(type))
            {
                return m_DelegateCache[type];
            }

            object delegateObject = null;

            foreach (var di in m_DelegateCollection.DependencyInjectors)
            {
                delegateObject = di(type);
                if (delegateObject != null) break;
            }

            if (delegateObject != null) m_DelegateCache.Add(type, delegateObject);

            return delegateObject;
        }

        public T Get<T>()
        {
            return (T)Get(typeof(T));
        }
    }
}