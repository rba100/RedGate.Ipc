using System;

namespace RedGate.Ipc.Channel
{
    public interface IChannelStream : IDisposable
    {
        int Read(byte[] buffer, int offset, int count);
        void Write(byte[] buffer, int offset, int count);
    }
}