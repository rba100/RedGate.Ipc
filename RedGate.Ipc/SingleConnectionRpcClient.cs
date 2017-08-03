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
        private readonly IConnection m_Connection;

        public SingleConnectionRpcClient(IConnection connection)
        {
            m_Connection = connection;
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
            if (!m_Connection.IsConnected) throw new ChannelFaultedException();
            var request = new RpcRequest(
                Guid.NewGuid().ToString(),
                methodInfo.DeclaringType.AssemblyQualifiedName,
                methodInfo.GetRpcSignature(), 
                args.Select(m_JsonSerializer.Serialize).ToArray());
            var response = m_Connection.RpcMessageBroker.Send(request);
            if (methodInfo.ReturnType == typeof(void)) return null;
            return m_JsonSerializer.Deserialize(methodInfo.ReturnType, response.ReturnValue);
        }

        private void ProxyDisposed()
        {
            // Ignore
        }

        public void Dispose()
        {
            // No resources
        }

        public long ConnectionRefreshCount => 1;
    }
}