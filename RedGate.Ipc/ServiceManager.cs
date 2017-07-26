using System.Collections.Generic;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ServiceManager : IServiceManager
    {
        private readonly List<IEndpoint> m_Endpoints = new List<IEndpoint>();
        private static readonly IJsonSerializer s_JsonSerializer = new TinyJsonSerializer();
        private readonly IConnectionFactory m_ConnectionFactory;
        private readonly IRpcRequestHandler m_RpcRequestHandler;

        public ServiceManager()
        {
            m_RpcRequestHandler = new RpcRequestHandler(s_JsonSerializer);
            m_ConnectionFactory = new ConnectionFactory(m_RpcRequestHandler);
        }

        public ServiceManager(IConnectionFactory connectionFactory)
        {
            m_RpcRequestHandler = new RpcRequestHandler(s_JsonSerializer);
            m_ConnectionFactory = connectionFactory;
        }

        public event ClientConnectedEventHandler ClientConnected = delegate { };

        public void AddEndpoint(IEndpoint endpoint)
        {
            m_Endpoints.Add(endpoint);
            endpoint.ChannelConnected += EndpointOnClientConnected;
        }

        private void EndpointOnClientConnected(ChannelConnectedEventArgs args)
        {
            var connection = m_ConnectionFactory.Create(args.ConnectionId, args.ChannelStream);
            ClientConnected.Invoke(new ConnectedEventArgs(connection));
        }

        public void Start()
        {
            foreach(var endpoint in m_Endpoints) endpoint.Start();
        }

        public void Stop()
        {
            foreach (var endpoint in m_Endpoints) endpoint.Stop();
        }

        public void Register<T>(object implementation)
        {
            m_RpcRequestHandler.Register<T>(implementation);
        }
    }
}