using System;

namespace RedGate.Ipc.Channel
{
    public interface IChannelStreamClientProvider : IDisposable
    {
        IChannelStream Connect();
    }
}