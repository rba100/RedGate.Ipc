using System;
using System.Threading;

namespace RedGate.Ipc.Channel
{
    public class ChannelMessageStream : IChannelMessageStream
    {
        private IChannelStream m_Stream;
        private readonly object m_WriteLock = new object();
        private readonly object m_ReadLock = new object();

        private const int c_HeaderSize = sizeof(Int32);

        private int m_Disposed;

        public ChannelMessageStream(IChannelStream stream)
        {
            m_Stream = stream;
        }

        public void Write(byte[] payload)
        {
            lock (m_WriteLock)
            {
                var header = EncodeHeader(payload.Length);
                WriteAll(header, c_HeaderSize);
                WriteAll(payload, payload.Length);
            }
        }

        public byte[] Read()
        {
            lock (m_ReadLock)
            {
                var header = new byte[c_HeaderSize];
                if (!ReadAll(header, c_HeaderSize)) return null;
                var payloadSize = DecodeHeader(header);
                var payloadBuffer = new byte[payloadSize];
                if (!ReadAll(payloadBuffer, payloadSize)) return null;
                return payloadBuffer;
            }
        }

        private byte[] EncodeHeader(int payloadSize)
        {
            // Little endian 
            return BitConverter.GetBytes(payloadSize);
        }

        private int DecodeHeader(byte[] header)
        {
            // Little endian 
            return BitConverter.ToInt32(header, 0);
        }

        private void WriteAll(byte[] buffer, int totalBytes)
        {
            var stream = m_Stream;
            if (stream == null) throw new ObjectDisposedException(GetType().FullName);
            try
            {
                m_Stream.Write(buffer, 0, totalBytes);
            }
            catch (ChannelFaultedException)
            {
                Dispose();
                throw new ChannelFaultedException("Could not write message");
            }
        }

        private bool ReadAll(byte[] buffer, int totalBytes)
        {
            var stream = m_Stream;
            if (stream == null) throw new ObjectDisposedException(GetType().FullName);
            int bytesLeft = totalBytes;
            try
            {
                while (bytesLeft > 0)
                {
                    var bytesRead = stream.Read(buffer, totalBytes - bytesLeft, bytesLeft);
                    if (bytesRead == 0) return false;
                    bytesLeft -= bytesRead;
                }
            }
            catch (ChannelFaultedException)
            {
                Dispose();
                throw new ChannelFaultedException("Could not read message");
            }
            return true;
        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref m_Disposed) == 1)
            {
                m_Stream.Dispose();
            }
        }
    }
}