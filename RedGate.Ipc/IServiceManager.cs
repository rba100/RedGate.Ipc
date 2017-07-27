using System;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IServiceManager
    {
        void Register<T>(object implementation);
        void RegisterDi(Func<Type, object> delegateFactory);

        void AddEndpoint(IEndpoint endpoint);
        void Start();
        void Stop();

        event ClientConnectedEventHandler ClientConnected;
    }


}
