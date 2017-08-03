using System;
using RedGate.Ipc.NamedPipes;
using RedGate.Ipc.Rpc;
using RedGate.Ipc.Tcp;

namespace RedGate.Ipc
{
    public class ClientBuilder : IClientBuilder
    {
        private readonly IDelegateProvider m_DelegateProvider = new DelegateProvider();

        public IRpcClient ConnectToNamedPipe(string pipeName)
        {
            var namedPipesClient = new NamedPipeEndpointClient(pipeName);
            var connectionFactory = new ConnectionFactory(m_DelegateProvider);
            var connectionProvider = new ReconnectingConnectionProvider(() => connectionFactory.Create(Guid.NewGuid().ToString(), namedPipesClient.Connect()));
            return new ReconnectingRpcClient(m_DelegateProvider, connectionProvider);
        }

        public IRpcClient ConnectToTcpSocket(string hostname, int portNumber)
        {
            var tcpProvider = new TcpEndpointClient(portNumber, hostname);
            var connectionFactory = new ConnectionFactory(m_DelegateProvider);
            var connectionProvider = new ReconnectingConnectionProvider(() => connectionFactory.Create(Guid.NewGuid().ToString(), tcpProvider.Connect()));
            return new ReconnectingRpcClient(m_DelegateProvider, connectionProvider);
        }

        public void AddDelegateFactory(Func<Type, object> delegateFactory)
        {
            m_DelegateProvider.AddDelegateFactory(delegateFactory);
        }

        public void AddTypeAlias(string alias, Type interfaceType)
        {
            m_DelegateProvider.AddTypeAlias(alias, interfaceType);
        }
    }
}