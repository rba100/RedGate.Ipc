using System;
using RedGate.Ipc.ImportedCode;
using RedGate.Ipc.Json;

namespace RedGate.Ipc.Rpc
{
    internal class RpcProxyFactory
    {
        private readonly IJsonSerializer m_JsonSerializer;

        private readonly ProxyFactory m_ProxyFactory = new ProxyFactory();

        public RpcProxyFactory(IJsonSerializer jsonSerializer)
        {
            if (jsonSerializer == null) throw new ArgumentNullException(nameof(jsonSerializer));
            m_JsonSerializer = jsonSerializer;
        }

        public T CreateProxy<T>(IConnection connection)
        {
            return m_ProxyFactory.Create<T>(new RpcCallHandler<T>(connection.RpcMessageBroker, m_JsonSerializer));
        }
    }
}
