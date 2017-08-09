using System;
using System.Collections.Generic;
using System.Reflection;
using RedGate.Ipc.Proxy;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ReconnectingRpcClient : IRpcClient
    {
        private readonly IDelegateCollection m_DelegateCollection;
        private readonly IConnectionProvider m_ConnectionProvider;
        private IRpcRequestBridge m_RpcRequestBridge;

        private readonly Dictionary<object, Action<object>> m_InitialisationFunctions = new Dictionary<object, Action<object>>();
        private readonly Dictionary<Type, long> m_LastConnectionForInterfaceType = new Dictionary<Type, long>();

        private static readonly ProxyFactory s_ProxyFactory = new ProxyFactory();
        private bool m_IsDisposed;

        public int ConnectionTimeoutMs { get; set; } = 6000;

        internal ReconnectingRpcClient(IDelegateCollection delegateCollection, IConnectionProvider connectionProvider)
        {
            if (delegateCollection == null) throw new ArgumentNullException(nameof(delegateCollection));
            if (connectionProvider == null) throw new ArgumentNullException(nameof(connectionProvider));

            m_DelegateCollection = delegateCollection;
            m_ConnectionProvider = connectionProvider;
        }

        public T CreateProxy<T>(Action<T> initialisation = null)
        {
            ICallHandler callHandler = new DelegatingCallHandler(HandleCall, ProxyDisposed);
            var proxy = s_ProxyFactory.Create<T>(callHandler);
            if (initialisation != null)
            {
                m_InitialisationFunctions[proxy] = o => initialisation((T) o);
            }
            return proxy;
        }

        public T CreateProxy<T, TConnectionFailureExceptionType>(Action<T> initialisation = null) where TConnectionFailureExceptionType : Exception
        {
            ICallHandler callHandler = new DelegatingCallHandler(HandleCall, ProxyDisposed, typeof(TConnectionFailureExceptionType));
            var proxy = s_ProxyFactory.Create<T>(callHandler);
            if (initialisation != null)
            {
                m_InitialisationFunctions[proxy] = o => initialisation((T)o);
            }
            return proxy;
        }

        public void AddDelegateFactory(Func<Type, object> delegateFactory)
        {
            m_DelegateCollection.DependencyInjectors.Add(delegateFactory);
        }

        public void AddTypeAlias(string assemblyQualifiedName, Type type)
        {
            m_DelegateCollection.TypeAliases.Add(assemblyQualifiedName, type);
        }

        private object HandleCall(object sender, MethodInfo methodInfo, object[] args)
        {
            if (m_IsDisposed) throw new ObjectDisposedException(typeof(ReconnectingRpcClient).FullName, $"The underlying {nameof(ReconnectingRpcClient)} was disposed.");

            var connection = m_ConnectionProvider.TryGetConnection(ConnectionTimeoutMs);
            if (connection == null) throw new ChannelFaultedException("Unable to connect.");
            var intType = methodInfo.DeclaringType;
            if (!m_LastConnectionForInterfaceType.ContainsKey(intType))
                m_LastConnectionForInterfaceType[intType] = -1;

            if (m_LastConnectionForInterfaceType[intType] != m_ConnectionProvider.ConnectionRefreshCount)
            {
                m_LastConnectionForInterfaceType[intType] = m_ConnectionProvider.ConnectionRefreshCount;
                m_RpcRequestBridge = new RpcRequestBridge(connection.RpcMessageBroker);
                m_InitialisationFunctions[sender]?.Invoke(sender);
            }

            return m_RpcRequestBridge.Call(methodInfo, args);
        }

        private void ProxyDisposed(object sender)
        {
            if (m_InitialisationFunctions.ContainsKey(sender))
                m_InitialisationFunctions.Remove(sender);
        }

        public void Dispose()
        {
            m_IsDisposed = true;
            m_ConnectionProvider?.Dispose();
        }
    }
}