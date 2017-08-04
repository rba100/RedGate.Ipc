using System;
using System.Collections.Generic;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ServiceHostBuilder : IServiceHostBuilder
    {
        private readonly List<IEndpoint> m_Endpoints = new List<IEndpoint>();
        private readonly IDelegateCollection m_DelegateCollection = new DelegateCollection();
        private readonly List<ClientConnectedEventHandler> m_ClientConnectedEventHandlers = new List<ClientConnectedEventHandler>();

        public void AddEndpoint(IEndpoint endpoint)
        {
            m_Endpoints.Add(endpoint);
        }

        public IServiceHost Create()
        {
            var serviceHost = new ServiceHost(
                m_Endpoints,
                m_DelegateCollection,
                m_ClientConnectedEventHandlers);

            return serviceHost;
        }

        public void AddDelegateFactory(Func<Type, object> delegateFactory)
        {
            m_DelegateCollection.DependencyInjectors.Add(delegateFactory);
        }

        public void AddTypeAlias(string alias, Type interfaceType)
        {
            m_DelegateCollection.TypeAliases.Add(alias, interfaceType);
        }

        public void AddDuplexDelegateFactory<TServiceContract, TClientCallback>(Func<TClientCallback, TServiceContract> serviceFactory) 
        {
            var serviceType = typeof(TServiceContract);
            var callbackType = typeof(TClientCallback);

            if (m_DelegateCollection.DuplexDelegateFactories.ContainsKey(serviceType))
            {
                throw new InvalidOperationException($"Duplicate factory for service '{serviceType.Name}'." +
                                                    $" Only one factory per service type can be registered.");
            }

            m_DelegateCollection.DuplexDelegateFactories.Add(
                serviceType,
                new KeyValuePair<Type, Func<object, object>>(
                    callbackType,
                    o => serviceFactory((TClientCallback)o)));
        }

        public void AddClientConnectedHandler(ClientConnectedEventHandler handler)
        {
            m_ClientConnectedEventHandlers.Add(handler);
        }
    }
}