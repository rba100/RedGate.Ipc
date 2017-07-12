using System;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.Channel
{
    internal interface IChannelMessageDispatcher : IDisposable
    {
        void Send(ChannelMessage channelMessage);
        void Start();
        void Stop();

        event DisconnectedEventHandler Disconnected;
    }
}