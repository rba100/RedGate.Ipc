using System;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IServiceManager
    {
        void Register<T>(object implementation);

        /// <summary>
        /// The delegateFactory will be called once per type per connection
        /// and the result cached for that connection. It will not be disposed
        /// when the connection is disposed.
        /// </summary>
        void RegisterDi(Func<Type, object> delegateFactory);

        void AddEndpoint(IEndpoint endpoint);
        void Start();
        void Stop();

        event ClientConnectedEventHandler ClientConnected;
    }


}
