using System;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.Channel
{
    internal interface IChannelMessageDispatcher : IDisposable
    {
        event DisconnectedEventHandler Disconnected;
    }
}