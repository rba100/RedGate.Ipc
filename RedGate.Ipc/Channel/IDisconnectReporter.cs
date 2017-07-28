using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.Channel
{
    public interface IDisconnectReporter
    {
        event DisconnectedEventHandler Disconnected;
    }
}