using System;
using System.Net.Sockets;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.Tcp
{
    public class TcpRpcClient : IRpcClient
    {
        private readonly int m_PortNumber;
        private readonly string m_HostName;
        private readonly IRpcRequestHandler m_RpcRequestHandler;
        private readonly RpcProxyFactory m_ProxyFactory;
        private readonly IConnectionFactory m_ConnectionFactory;

        public TcpRpcClient(int portNumber, string hostName)
        {
            m_PortNumber = portNumber;
            m_HostName = hostName;

            var serialiser = new TinyJsonSerializer();
            m_RpcRequestHandler = new RpcRequestHandler(serialiser);
            m_ConnectionFactory = new ConnectionFactory(m_RpcRequestHandler);
            m_ProxyFactory = new RpcProxyFactory(serialiser);
        }

        public T CreateProxy<T>()
        {
            var client = new TcpClient();
            client.Connect(m_HostName, m_PortNumber);
            var connection = m_ConnectionFactory.Create(Guid.NewGuid().ToString(), new SimpleStream(client.GetStream()));
            return m_ProxyFactory.CreateProxy<T>(connection);
        }

        public void Register<T>(object implementation)
        {
            m_RpcRequestHandler.Register<T>(implementation);
        }
    }
}