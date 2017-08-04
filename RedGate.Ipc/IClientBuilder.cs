using System;

namespace RedGate.Ipc
{
    public interface IClientBuilder
    {
        IRpcClient ConnectToNamedPipe(string pipeName);
        IRpcClient ConnectToTcpSocket(string hostname, int portNumber);
        void AddCallbackHandler<TCallback>(Func<TCallback> callbackFactory);
        void AddTypeAlias(string alias, Type interfaceType);
    }
}