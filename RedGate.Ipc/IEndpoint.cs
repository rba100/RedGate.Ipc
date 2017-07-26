using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IEndpoint
    {
        event ChannelConnectedEventHandler ChannelConnected;
        void Start();
        void Stop();
    }
}