using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IServiceManager
    {
        void Register<T>(object implementation);
        void AddEndpoint(IEndpointListener endpointListener);
        void Start();
        void Stop();

        event ClientConnectedEventHandler ClientConnected;
    }


}
