using System;
using System.IO;

namespace RedGate.Ipc.Channel
{
    class ChannelStreamCustomDispose : IChannelStream
    {
        private Stream m_Stream;
        private readonly Action m_DisposeAction;

        public ChannelStreamCustomDispose(Stream stream, Action disposeAction)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));

            m_Stream = stream;
            m_DisposeAction = disposeAction;
        }

        public void Dispose()
        {
            try
            {
                m_DisposeAction();
            }
            catch
            {
                //
            }
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