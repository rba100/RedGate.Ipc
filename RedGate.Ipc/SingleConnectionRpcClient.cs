using System;
using System.Linq;
using System.Reflection;

using RedGate.Ipc.ImportedCode;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class SingleConnectionRpcClient : IRpcClient
    {
        private readonly IJsonSerializer m_JsonSerializer = new TinyJsonSerializer();
        private static readonly ProxyFactory s_ProxyFactory = new ProxyFactory();
        private readonly IRpcMessageBroker m_MessageBroker;

        public SingleConnectionRpcClient(IRpcMessageBroker messageBroker)
        {
            m_MessageBroker = messageBroker;
        }

        public object CreateProxy(Type interfaceType)
        {
            return s_ProxyFactory.Create(
                interfaceType,
                new DelegatingCallHandler(
                    HandleCallSingleConnection,
                    ProxyDisposed));
        }

        public T CreateProxy<T>()
        {
            return s_ProxyFactory.Create<T>(new DelegatingCallHandler(
                HandleCallSingleConnection,
                ProxyDisposed));
        }

        public T CreateProxy<T, TConnectionFailureExceptionType>() where TConnectionFailureExceptionType : Exception
        {
            return s_ProxyFactory.Create<T>(new DelegatingCallHandler(
                HandleCallSingleConnection,
                ProxyDisposed, 
                typeof(TConnectionFailureExceptionType)));
        }

        private object HandleCallSingleConnection(MethodInfo methodInfo, object[] args)
        {
            var request = new RpcRequest(
                Guid.NewGuid().ToString(),
                methodInfo.DeclaringType.AssemblyQualifiedName,
                methodInfo.GetRpcSignature(), 
                args.Select(m_JsonSerializer.Serialize).ToArray());

            var response = m_MessageBroker.Send(request);
            if (methodInfo.ReturnType == typeof(void)) return null;
            return m_JsonSerializer.Deserialize(methodInfo.ReturnType, response.ReturnValue);
        }

        private void ProxyDisposed()
        {
            // Ignore
        }

        public void Dispose()
        {
            // No resources, connection should be closed elsewhere.
        }

        public long ConnectionRefreshCount => 1;
    }
}