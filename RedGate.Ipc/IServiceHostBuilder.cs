using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IServiceHostBuilder : IDelegateRegistrar
    {
        void AddEndpoint(IEndpoint endpoint);

        IServiceHost Create();

        event ClientConnectedEventHandler ClientConnected;
    }
}
