using System;
using RedGate.Ipc.ImportedCode;
using RedGate.Ipc.Proxy;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    internal class SingleConnectionRpcClient : IRpcClient
    {
        private static readonly ProxyFactory s_ProxyFactory = new ProxyFactory();
        private readonly IRpcRequestBridge m_RpcRequestBridge;

        internal SingleConnectionRpcClient(IRpcRequestBridge rpcRequestBridge)
        {
            m_RpcRequestBridge = rpcRequestBridge;
        }

        public object CreateProxy(Type interfaceType)
        {
            return s_ProxyFactory.Create(
                interfaceType,
                new DelegatingCallHandler(
                    m_RpcRequestBridge.Call,
                    ProxyDisposed));
        }

        public T CreateProxy<T>()
        {
            return s_ProxyFactory.Create<T>(new DelegatingCallHandler(
                m_RpcRequestBridge.Call,
                ProxyDisposed));
        }

        public T CreateProxy<T, TConnectionFailureExceptionType>() where TConnectionFailureExceptionType : Exception
        {
            return s_ProxyFactory.Create<T>(new DelegatingCallHandler(
                m_RpcRequestBridge.Call,
                ProxyDisposed, 
                typeof(TConnectionFailureExceptionType)));
        }

        private void ProxyDisposed()
        {
            // Ignore
        }

        public void Dispose()
        {
            // No resources, connection should be closed elsewhere.
        }
    }
}