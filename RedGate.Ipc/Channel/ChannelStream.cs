using System;
using System.IO;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.Channel
{
    class ChannelStream : IChannelStream
    {
        private event DisconnectedEventHandler DisconnectedImpl = delegate { };

        private readonly Stream m_Stream;
        private readonly Action m_CustomDisposeAction;

        private volatile bool m_IsDisposed;
        private readonly object m_IsDisposedLock = new object();

        public ChannelStream(Stream stream, Action customCustomDisposeAction)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (customCustomDisposeAction == null) throw new ArgumentNullException(nameof(customCustomDisposeAction));

            m_Stream = stream;
            m_CustomDisposeAction = customCustomDisposeAction;
        }

        public ChannelStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            m_Stream = stream;
        }

        public event DisconnectedEventHandler Disconnected
        {
            add
            {
                lock (m_IsDisposedLock)
                {
                    DisconnectedImpl += value;
                    if (m_IsDisposed)
                    {
                        value();
                    }
                }
            }

            remove
            {
                DisconnectedImpl -= value;
            }
        }

        public void Dispose()
        {
            lock (m_IsDisposedLock)
            {
                if (m_IsDisposed) return;
                m_IsDisposed = true;
            }

            if (m_CustomDisposeAction == null)
            {
                m_Stream.Dispose();
            }
            else
            {

                try
                {
                    m_CustomDisposeAction();
                }
                catch
                {
                    //
                }
            }

            try
            {
                DisconnectedImpl();
            }
            catch
            {
                //
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                var bytes = m_Stream.Read(buffer, offset, count);
                if (bytes == 0) throw new ChannelFaultedException();
                return bytes;
            }
            catch (IOException)
            {
                throw OnFailure();
            }
            catch (ObjectDisposedException)
            {
                throw OnFailure();
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                m_Stream.Write(buffer, offset, count);
                m_Stream.Flush();
            }
            catch (IOException)
            {
                throw OnFailure();
            }
            catch (ObjectDisposedException)
            {
                throw OnFailure();
            }
        }

        private Exception OnFailure()
        {
            Dispose();
            return new ChannelFaultedException();
        }
    }
}