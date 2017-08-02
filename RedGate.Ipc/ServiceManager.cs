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
        private readonly IDelegateProvider m_DelegateProvider;

        public ServiceManager()
        {
            m_DelegateProvider = new DelegateProvider();
            m_ConnectionFactory = new ConnectionFactory(m_DelegateProvider);
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
            foreach (var endpoint in m_Endpoints) endpoint.Dispose();
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

        private void EndpointOnClientConnected(ChannelConnectedEventArgs args)
        {
            var connection = m_ConnectionFactory.Create(args.ConnectionId, args.ChannelStream);
            ClientConnected.Invoke(new ConnectedEventArgs(connection));
        }
    }
}