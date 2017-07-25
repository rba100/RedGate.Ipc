using System;

namespace RedGate.Ipc.Channel
{
    public interface IChannelMessageStream : IDisposable
    {
        void Write(byte[] payload);

        /// <summary>
        /// Returns null if stream will never return more data. Consumer shouold 
        /// </summary>
        byte[] Read();
    }
}