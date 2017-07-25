using System;

namespace RedGate.Ipc.Channel
{
    public interface IChannelStreamProvider : IDisposable
    {
        IChannelStream Connect();
    }
}