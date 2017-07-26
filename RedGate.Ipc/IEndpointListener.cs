using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IEndpointListener
    {
        event ChannelConnectedEventHandler ChannelConnected;
        void Start();
        void Stop();
    }
}