using System;
using System.Linq;
using System.Reflection;

using RedGate.Ipc.ImportedCode;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ReconnectingRpcClient : IRpcClient
    {
        private readonly IDelegateProvider m_DelegateProvider;
        private readonly IConnectionProvider m_ConnectionProvider;
        private readonly IJsonSerializer m_JsonSerializer;

        private static readonly ProxyFactory s_ProxyFactory = new ProxyFactory();
        private bool m_IsDisposed;

        public int ConnectionTimeoutMs { get; set; } = 6000;

        public long ConnectionRefreshCount => m_ConnectionProvider.ConnectionRefreshCount;

        internal ReconnectingRpcClient(
            IDelegateProvider delegateProvider,
            IConnectionProvider connectionProvider)
        {
            if (delegateProvider == null) throw new ArgumentNullException(nameof(delegateProvider));
            if (connectionProvider == null) throw new ArgumentNullException(nameof(connectionProvider));

            m_DelegateProvider = delegateProvider;
            m_ConnectionProvider = connectionProvider;
            m_JsonSerializer = new TinyJsonSerializer();
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

        public void Register<T>(object implementation)
        {
            m_DelegateProvider.Register<T>(implementation);
        }

        public void RegisterDi(Func<Type, object> delegateFactory)
        {
            m_DelegateProvider.RegisterDi(delegateFactory);
        }

        public void RegisterAlias(string assemblyQualifiedName, Type type)
        {
            m_DelegateProvider.RegisterAlias(assemblyQualifiedName, type);
        }

        private object HandleCallReconnectOnFailure(MethodInfo methodInfo, object[] args)
        {
            if (m_IsDisposed) throw new ObjectDisposedException(typeof(ReconnectingRpcClient).FullName, $"The underlying {nameof(ReconnectingRpcClient)} was disposed.");
            var request = new RpcRequest(
                    Guid.NewGuid().ToString(),
                    methodInfo.DeclaringType.AssemblyQualifiedName,
                    methodInfo.GetRpcSignature(), 
                    args.Select(m_JsonSerializer.Serialize).ToArray());
            var connection = m_ConnectionProvider.TryGetConnection(ConnectionTimeoutMs);
            if (m_IsDisposed) throw new ObjectDisposedException(typeof(ReconnectingRpcClient).FullName, $"The underlying {nameof(ReconnectingRpcClient)} was disposed.");
            if (connection == null) throw new ChannelFaultedException("Timed out trying to connect");
            var response = connection.RpcMessageBroker.Send(request);
            if (methodInfo.ReturnType == typeof(void)) return null;
            return m_JsonSerializer.Deserialize(methodInfo.ReturnType, response.ReturnValue);
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