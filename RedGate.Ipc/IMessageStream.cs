using System;

namespace RedGate.Ipc
{
    public interface IMessageStream : IDisposable
    {
        void Write(byte[] payload);

        /// <summary>
        /// Returns null if stream will never return more data.
        /// </summary>
        byte[] Read();
    }
}