using System;
using System.Collections.Generic;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    internal class ServiceHost : IServiceHost
    {
        private readonly List<IEndpoint> m_Endpoints;
        private readonly IConnectionFactory m_ConnectionFactory;

        private event ClientConnectedEventHandler ClientConnected = delegate { };

        public ServiceHost(
            IEnumerable<IEndpoint> endpoints,
            IEnumerable<KeyValuePair<Type, object>> delegates,
            IEnumerable<Func<Type,object>> delegateFactories,
            IEnumerable<KeyValuePair<string,Type>> interfaceAliases,
            IEnumerable<ClientConnectedEventHandler> handlers)
        {
            m_Endpoints = new List<IEndpoint>(endpoints);
            IDelegateProvider delegateProvider = new DelegateProvider();

            foreach (var pair in delegates)
            {
                delegateProvider.Register(pair.Key, pair.Value);
            }

            foreach (var factory in delegateFactories)
            {
                delegateProvider.RegisterDi(factory);
            }

            foreach (var pair in interfaceAliases)
            {
                delegateProvider.RegisterAlias(pair.Key, pair.Value);
            }

            foreach (var handler in handlers)
            {
                ClientConnected += handler;
            }

            m_ConnectionFactory = new ConnectionFactory(delegateProvider);

            foreach (var endpoint in m_Endpoints)
            {
                endpoint.ChannelConnected += EndpointOnClientConnected;
                endpoint.Start();
            }
        }

        private void EndpointOnClientConnected(ChannelConnectedEventArgs args)
        {
            var connection = m_ConnectionFactory.Create(args.ConnectionId, args.ChannelStream);
            ClientConnected.Invoke(new ConnectedEventArgs(connection));
        }

        public void Dispose()
        {
            foreach (var endpoint in m_Endpoints) endpoint.Dispose();
        }
    }
}