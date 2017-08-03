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
            var connectionProvider = new ConnectionProvider(() => connectionFactory.Create(Guid.NewGuid().ToString(), namedPipesClient.Connect()));
            return new ReconnectingRpcClient(m_DelegateProvider, connectionProvider);
        }

        public IRpcClient ConnectToTcpSocket(string hostname, int portNumber)
        {
            var tcpProvider = new TcpEndpointClient(portNumber, hostname);
            var connectionFactory = new ConnectionFactory(m_DelegateProvider);
            var connectionProvider = new ConnectionProvider(() => connectionFactory.Create(Guid.NewGuid().ToString(), tcpProvider.Connect()));
            return new ReconnectingRpcClient(m_DelegateProvider, connectionProvider);
        }

        public void Register<T>(object implementation)
        {
            m_DelegateProvider.Register<T>(implementation);
        }

        public void RegisterDi(Func<Type, object> delegateFactory)
        {
            m_DelegateProvider.RegisterDi(delegateFactory);
        }

        public void RegisterAlias(string alias, Type interfaceType)
        {
            m_DelegateProvider.RegisterAlias(alias, interfaceType);
        }
    }
}