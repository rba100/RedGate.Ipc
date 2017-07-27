using System;
using System.Collections.Generic;

using RedGate.Ipc.Channel;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ServiceManager : IServiceManager
    {
        public event ClientConnectedEventHandler ClientConnected = delegate { };

        private readonly List<IEndpoint> m_Endpoints = new List<IEndpoint>();
        private readonly IConnectionFactory m_ConnectionFactory;
        private readonly ITypeResolver m_TypeResolver;

        public ServiceManager()
        {
            m_TypeResolver = new TypeResolver();
            m_ConnectionFactory = new ConnectionFactory(m_TypeResolver);
        }

        public void AddEndpoint(IEndpoint endpoint)
        {
            m_Endpoints.Add(endpoint);
            endpoint.ChannelConnected += EndpointOnClientConnected;
        }

        public void Start()
        {
            foreach (var endpoint in m_Endpoints) endpoint.Start();
        }

        public void Stop()
        {
            foreach (var endpoint in m_Endpoints) endpoint.Stop();
        }

        public void Register<T>(object implementation)
        {
            m_TypeResolver.RegisterGlobal<T>(implementation);
        }

        public void RegisterDi(Func<Type, object> delegateFactory)
        {
            m_TypeResolver.RegisterDi(delegateFactory);
        }

        private void EndpointOnClientConnected(ChannelConnectedEventArgs args)
        {
            var connection = m_ConnectionFactory.Create(args.ConnectionId, args.ChannelStream);
            ClientConnected.Invoke(new ConnectedEventArgs(connection));
        }
    }
}