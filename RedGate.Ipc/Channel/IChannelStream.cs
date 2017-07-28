using System;

namespace RedGate.Ipc.Channel
{
    public interface IChannelStream : IDisposable, IDisconnectReporter
    {
        int Read(byte[] buffer, int offset, int count);
        void Write(byte[] buffer, int offset, int count);
    }
}