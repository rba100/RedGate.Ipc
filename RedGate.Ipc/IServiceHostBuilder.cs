using System;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IServiceHostBuilder : IDelegateRegistrar
    {
        /// <summary>
        /// Provides full two-way communication by providing the service factory with an instance of a callback
        /// to a service provided by the connected client.
        /// </summary>
        /// <typeparam name="TServiceContract">The type of the service offered by this host.</typeparam>
        /// <typeparam name="TClientCallback">The type of the service offered by the connecting client.</typeparam>
        /// <param name="serviceFactory">
        /// A factory method that takes an instance of TClientCallback and must build a connection-scoped handler
        /// of type TServiceContract.
        /// </param>
        void AddDuplexDelegateFactory<TServiceContract, TClientCallback>(
            Func<TClientCallback, TServiceContract> serviceFactory);

        void AddEndpoint(IEndpoint endpoint);

        IServiceHost Create();

        void AddClientConnectedHandler(ClientConnectedEventHandler handler);
    }
}
