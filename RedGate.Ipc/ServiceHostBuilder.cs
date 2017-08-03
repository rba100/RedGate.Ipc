using System;
using System.Collections.Generic;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public class ServiceHostBuilder : IServiceHostBuilder
    {

        private readonly List<IEndpoint> m_Endpoints = new List<IEndpoint>();

        private readonly List<Func<Type, object>> m_DelegateFactories = new List<Func<Type, object>>();
        private readonly Dictionary<string, Type> m_DelegateAliases = new Dictionary<string, Type>();
        private readonly List<ClientConnectedEventHandler> m_ClientConnectedEventHandlers = new List<ClientConnectedEventHandler>();

        public void AddEndpoint(IEndpoint endpoint)
        {
            m_Endpoints.Add(endpoint);
        }

        public event ClientConnectedEventHandler ClientConnected
        {
            add
            {
                m_ClientConnectedEventHandlers.Add(value);
            }
            remove
            {
                m_ClientConnectedEventHandlers.Remove(value);
            }
        }

        public IServiceHost Create()
        {
            var serviceHost = new ServiceHost(
                m_Endpoints,
                m_DelegateFactories,
                m_DelegateAliases,
                m_ClientConnectedEventHandlers);

            return serviceHost;
        }

        public void AddDelegateFactory(Func<Type, object> delegateFactory)
        {
            m_DelegateFactories.Add(delegateFactory);
        }

        public void AddTypeAlias(string alias, Type interfaceType)
        {
            m_DelegateAliases.Add(alias, interfaceType);
        }
    }
}