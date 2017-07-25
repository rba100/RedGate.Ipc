using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IConnectionFactory
    {
        IConnection Create(string connectionId, IChannelStream stream);
    }
}