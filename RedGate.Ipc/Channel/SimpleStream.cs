using System;
using System.IO;

namespace RedGate.Ipc.Channel
{
    internal class SimpleStream : IChannelStream
    {
        private Stream m_Stream;

        public SimpleStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            m_Stream = stream;
        }

        public void Dispose()
        {
            m_Stream?.Dispose();
            m_Stream = null;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return m_Stream?.Read(buffer, offset, count) ?? -1;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            m_Stream?.Write(buffer, offset, count);
            m_Stream?.Flush();
        }
    }
}