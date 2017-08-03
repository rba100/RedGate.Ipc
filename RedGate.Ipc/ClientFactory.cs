using System;
using RedGate.Ipc.NamedPipes;
using RedGate.Ipc.Rpc;
using RedGate.Ipc.Tcp;

namespace RedGate.Ipc
{
    public static class ClientFactory
    {
        public static IRpcClient ConnectToNamedPipe(string pipeName)
        {
            var namedPipesClient = new NamedPipeEndpointClient(pipeName);
            var typeResolver = new DelegateProvider();
            var connectionFactory = new ConnectionFactory(typeResolver);
            var connectionProvider = new ConnectionProvider(() => connectionFactory.Create(Guid.NewGuid().ToString(), namedPipesClient.Connect()));
            return new ReconnectingRpcClient(typeResolver, connectionProvider);
        }

        public static IRpcClient ConnectToTcpSocket(string hostname, int portNumber)
        {
            var tcpProvider = new TcpEndpointClient(portNumber, hostname);
            var typeResolver = new DelegateProvider();
            var connectionFactory = new ConnectionFactory(typeResolver);
            var connectionProvider = new ConnectionProvider(() => connectionFactory.Create(Guid.NewGuid().ToString(), tcpProvider.Connect()));
            return new ReconnectingRpcClient(typeResolver, connectionProvider);
        }
    }
}