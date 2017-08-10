using System;
using RedGate.Ipc.Channel;
using RedGate.Ipc.NamedPipes;
using RedGate.Ipc.Rpc;
using RedGate.Ipc.Tcp;

namespace RedGate.Ipc
{
    public class ClientBuilder : IClientBuilder
    {
        private readonly IDelegateCollection m_DelegateCollection = new DelegateCollection();

        public IRpcClient ConnectToNamedPipe(string pipeName)
        {
            var namedPipesClient = new NamedPipeEndpointClient(pipeName);
            var connectionFactory = new ConnectionFactory(m_DelegateCollection);
            var connectionProvider = new ReconnectingConnectionProvider(() => connectionFactory.Create(Guid.NewGuid().ToString(), namedPipesClient.Connect()));
            return new ReconnectingRpcClient(m_DelegateCollection, connectionProvider, new TaskLauncherNet35());
        }

        public IRpcClient ConnectToTcpSocket(string hostname, int portNumber)
        {
            var tcpProvider = new TcpEndpointClient(portNumber, hostname);
            var connectionFactory = new ConnectionFactory(m_DelegateCollection);
            var connectionProvider = new ReconnectingConnectionProvider(() => connectionFactory.Create(Guid.NewGuid().ToString(), tcpProvider.Connect()));
            return new ReconnectingRpcClient(m_DelegateCollection, connectionProvider, new TaskLauncherNet35());
        }

        public void AddCallbackHandler<TCallback>(TCallback callback)
        {
            Func<Type, object> wrapper = type => type == typeof(TCallback) ? (object) callback : null;
            m_DelegateCollection.DependencyInjectors.Add(wrapper);
        }

        public void AddTypeAlias(string alias, Type interfaceType)
        {
            m_DelegateCollection.TypeAliases.Add(alias, interfaceType);
        }
    }
}