using System;
using System.Linq;
using System.Reflection;

using RedGate.Ipc.ImportedCode;
using RedGate.Ipc.Json;
using RedGate.Ipc.NamedPipes;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class RpcClient : IRpcClient
    {
        private readonly IRpcRequestHandler m_RequestHandler;
        private readonly IClientConnectionAgent m_ClientConnectionAgent;
        private readonly IJsonSerializer m_JsonSerializer;

        private readonly ICallHandler m_CallHandler;

        private static readonly ProxyFactory s_ProxyFactory = new ProxyFactory();
        private bool m_IsDisposed;

        internal RpcClient(
            IRpcRequestHandler requestHandler,
            IClientConnectionAgent clientConnectionAgent)
        {
            if (requestHandler == null) throw new ArgumentNullException(nameof(requestHandler));
            if (clientConnectionAgent == null) throw new ArgumentNullException(nameof(clientConnectionAgent));

            m_CallHandler = new DelegatingCallHandler(HandleCall, ProxyDisposed);

            m_RequestHandler = requestHandler;
            m_ClientConnectionAgent = clientConnectionAgent;
            m_JsonSerializer = new TinyJsonSerializer();
        }

        public static IRpcClient CreateNamedPipeClient(string pipeName)
        {
            var namedPipesClient = new NamedPipesChannelStreamProvider(pipeName);
            var requestHandler = new RpcRequestHandler();
            var connectionFactory = new ConnectionFactory(requestHandler);
            var clientAgent = new ClientConnectionAgent(() => connectionFactory.Create(Guid.NewGuid().ToString(), namedPipesClient.Connect()), null);
            return new RpcClient(requestHandler, clientAgent);
        }

        public T CreateProxy<T>()
        {
            return s_ProxyFactory.Create<T>(m_CallHandler);
        }

        public void Register<T>(object implementation)
        {
            m_RequestHandler.Register<T>(implementation);
        }

        private object HandleCall(MethodInfo methodInfo, object[] args)
        {
            if(m_IsDisposed) throw new ObjectDisposedException(typeof(RpcClient).FullName, "The underlying RpcClient was disposed.");
            var connection = m_ClientConnectionAgent.TryGetConnection(5000);
            // TODO: use correct exception type
            if (connection == null) throw new Exception("Could not connect");
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
            m_ClientConnectionAgent?.Dispose();
        }
    }
}