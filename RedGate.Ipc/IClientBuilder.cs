namespace RedGate.Ipc
{
    public interface IClientBuilder : IDelegateRegistrar
    {
        IRpcClient ConnectToNamedPipe(string pipeName);
        IRpcClient ConnectToTcpSocket(string hostname, int portNumber);
    }
}