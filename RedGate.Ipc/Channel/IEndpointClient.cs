using System;

namespace RedGate.Ipc.Channel
{
    public interface IEndpointClient : IDisposable
    {
        IChannelStream Connect();
    }
}