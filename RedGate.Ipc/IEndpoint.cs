using System;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IEndpoint : IDisposable
    {
        event ChannelConnectedEventHandler ChannelConnected;
        void Start();
    }
}