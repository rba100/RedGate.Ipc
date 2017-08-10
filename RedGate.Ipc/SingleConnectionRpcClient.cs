using System;
using System.Reflection;
using RedGate.Ipc.Proxy;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    internal class SingleConnectionRpcClient : IRpcClient
    {
        private static readonly ProxyFactory s_ProxyFactory = new ProxyFactory();

        private readonly IRpcMessageBroker m_MessageBroker;

        internal SingleConnectionRpcClient(IRpcMessageBroker messageBroker)
        {
            if (messageBroker == null) throw new ArgumentNullException(nameof(messageBroker));
            m_MessageBroker = messageBroker;
        }

        public object CreateProxy(Type interfaceType)
        {
            return s_ProxyFactory.Create(
                interfaceType,
                new DelegatingCallHandler(
                    HandleCall,
                    ProxyDisposed));
        }

        public T CreateProxy<T>(Action<T> initialisation = null)
        {
            var proxy = s_ProxyFactory.Create<T>(new DelegatingCallHandler(
                HandleCall,
                ProxyDisposed));
            initialisation?.Invoke(proxy);
            return proxy;
        }

        public T CreateProxy<T, TConnectionFailureExceptionType>(Action<T> initialisation = null) where TConnectionFailureExceptionType : Exception
        {
            var proxy = s_ProxyFactory.Create<T>(new DelegatingCallHandler(
                HandleCall,
                ProxyDisposed, 
                typeof(TConnectionFailureExceptionType)));
            initialisation?.Invoke(proxy);
            return proxy;
        }

        private object HandleCall(object sender, MethodInfo methodInfo, object[] args)
        {
            return m_MessageBroker.Call(methodInfo, args);
        }

        private void ProxyDisposed(object sender)
        {
            // Ignore
        }

        public void Dispose()
        {
            // No resources, connection should be closed elsewhere.
        }
    }
}