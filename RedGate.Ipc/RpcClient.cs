using System;
using System.Linq;
using System.Reflection;

using RedGate.Ipc.ImportedCode;
using RedGate.Ipc.Json;
using RedGate.Ipc.NamedPipes;
using RedGate.Ipc.Rpc;
using RedGate.Ipc.Tcp;

namespace RedGate.Ipc
{
    public class RpcClient : IRpcClient
    {
        private readonly IRpcRequestHandler m_RequestHandler;
        private readonly IReliableConnectionAgent m_ReliableConnectionAgent;
        private readonly IJsonSerializer m_JsonSerializer;

        private static readonly ProxyFactory s_ProxyFactory = new ProxyFactory();
        private bool m_IsDisposed;

        internal RpcClient(
            IRpcRequestHandler requestHandler,
            IReliableConnectionAgent reliableConnectionAgent)
        {
            if (requestHandler == null) throw new ArgumentNullException(nameof(requestHandler));
            if (reliableConnectionAgent == null) throw new ArgumentNullException(nameof(reliableConnectionAgent));

            m_RequestHandler = requestHandler;
            m_ReliableConnectionAgent = reliableConnectionAgent;
            m_JsonSerializer = new TinyJsonSerializer();
        }

        public static IRpcClient CreateNamedPipeClient(string pipeName)
        {
            var namedPipesClient = new NamedPipeEndpointClient(pipeName);
            var requestHandler = new RpcRequestHandler();
            var connectionFactory = new ConnectionFactory(requestHandler);
            var clientAgent = new ReliableConnectionAgent(() => connectionFactory.Create(Guid.NewGuid().ToString(), namedPipesClient.Connect()), null);
            return new RpcClient(requestHandler, clientAgent);
        }

        public static IRpcClient CreateTcpClient(string hostname, int portNumber)
        {
            var tcpProvider = new TcpEndpointClient(portNumber, hostname);
            var requestHandler = new RpcRequestHandler();
            var connectionFactory = new ConnectionFactory(requestHandler);
            var clientAgent = new ReliableConnectionAgent(() => connectionFactory.Create(Guid.NewGuid().ToString(), tcpProvider.Connect()), null);
            return new RpcClient(requestHandler, clientAgent);
        }

        public T CreateProxy<T>()
        {
            return s_ProxyFactory.Create<T>(new DelegatingCallHandler(HandleCall, ProxyDisposed));
        }

        public T CreateProxy<T, TConnectionFailureExceptionType>() where TConnectionFailureExceptionType : Exception
        {
            return s_ProxyFactory.Create<T>(new DelegatingCallHandler(HandleCall, ProxyDisposed, typeof(TConnectionFailureExceptionType)));
        }

        public void Register<T>(object implementation)
        {
            m_RequestHandler.Register<T>(implementation);
        }

        private object HandleCall(MethodInfo methodInfo, object[] args)
        {
            if(m_IsDisposed) throw new ObjectDisposedException(typeof(RpcClient).FullName, "The underlying RpcClient was disposed.");
            var connection = m_ReliableConnectionAgent.TryGetConnection(5000);
            // TODO: use correct exception type
            if (connection == null) throw new ChannelFaultedException("Timed out trying to connect");
            var response = connection.RpcMessageBroker.Send(
                new RpcRequest(
                    Guid.NewGuid().ToString(),
                    methodInfo.DeclaringType.FullName,
                    methodInfo.Name, args.Select(m_JsonSerializer.Serialize).ToArray()));
            if (methodInfo.ReturnType == typeof(void)) return null;
            return m_JsonSerializer.Deserialize(methodInfo.ReturnType, response.ReturnValue);
        }

        private void ProxyDisposed()
        {
            // Ignore
        }

        public void Dispose()
        {
            m_IsDisposed = true;
            m_ReliableConnectionAgent.Dispose();
        }
    }
}