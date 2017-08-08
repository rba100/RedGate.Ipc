using System;
using System.Reflection;
using RedGate.Ipc.Json;
using RedGate.Ipc.Proxy;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ReconnectingRpcClient : IRpcClient
    {
        private readonly IDelegateCollection m_DelegateCollection;
        private readonly IConnectionProvider m_ConnectionProvider;
        private IRpcRequestBridge m_RpcRequestBridge;

        private static readonly ProxyFactory s_ProxyFactory = new ProxyFactory();
        private bool m_IsDisposed;

        public int ConnectionTimeoutMs { get; set; } = 6000;

        public long ConnectionRefreshCount => m_ConnectionProvider.ConnectionRefreshCount;
        private long m_LastConnectionId = -1;

        internal ReconnectingRpcClient(IDelegateCollection delegateCollection, IConnectionProvider connectionProvider)
        {
            if (delegateCollection == null) throw new ArgumentNullException(nameof(delegateCollection));
            if (connectionProvider == null) throw new ArgumentNullException(nameof(connectionProvider));

            m_DelegateCollection = delegateCollection;
            m_ConnectionProvider = connectionProvider;
        }

        public T CreateProxy<T>()
        {
            ICallHandler callHandler = new DelegatingCallHandler(HandleCallReconnectOnFailure, ProxyDisposed);
            return s_ProxyFactory.Create<T>(callHandler);
        }

        public T CreateProxy<T, TConnectionFailureExceptionType>() where TConnectionFailureExceptionType : Exception
        {
            ICallHandler callHandler = new DelegatingCallHandler(HandleCallReconnectOnFailure, ProxyDisposed, typeof(TConnectionFailureExceptionType));
            return s_ProxyFactory.Create<T>(callHandler);
        }

        public void AddDelegateFactory(Func<Type, object> delegateFactory)
        {
            m_DelegateCollection.DependencyInjectors.Add(delegateFactory);
        }

        public void AddTypeAlias(string assemblyQualifiedName, Type type)
        {
            m_DelegateCollection.TypeAliases.Add(assemblyQualifiedName, type);
        }

        private object HandleCallReconnectOnFailure(MethodInfo methodInfo, object[] args)
        {
            if (m_IsDisposed) throw new ObjectDisposedException(typeof(ReconnectingRpcClient).FullName, $"The underlying {nameof(ReconnectingRpcClient)} was disposed.");

            var interfaceName = methodInfo.DeclaringType?.AssemblyQualifiedName;
            if (interfaceName == null) throw new ContractMismatchException("Maybe loosen off on the generics a bit? There's only so much magic an API can be.");

            var connection = m_ConnectionProvider.TryGetConnection(ConnectionTimeoutMs);
            if (connection == null) throw new ChannelFaultedException("Unable to connect.");
            if (m_LastConnectionId != ConnectionRefreshCount)
            {
                m_RpcRequestBridge = new RpcRequestBridge(connection.RpcMessageBroker);
            }

            return m_RpcRequestBridge.Call(methodInfo, args);
        }

        private void ProxyDisposed()
        {
            // Ignore
        }

        public void Dispose()
        {
            m_IsDisposed = true;
            m_ConnectionProvider?.Dispose();
        }
    }
}