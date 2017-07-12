using System.IO;

namespace RedGate.Ipc
{
    public interface IConnectionFactory
    {
        IConnection Create(string connectionId, Stream stream);
        event ClientDisconnectedEventHandler ClientDisconnected;
    }
}