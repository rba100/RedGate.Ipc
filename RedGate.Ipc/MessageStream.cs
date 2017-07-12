using System;
using System.IO;

namespace RedGate.Ipc
{
    public class MessageStream : IMessageStream
    {
        private readonly Stream m_Stream;
        private readonly object m_WriteLock = new object();
        private readonly object m_ReadLock = new object();

        private const int c_HeaderSize = sizeof(Int32);

        public MessageStream(Stream stream)
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
            return BitConverter.GetBytes(payloadSize);
        }

        private int DecodeHeader(byte[] header)
        {
            return BitConverter.ToInt32(header, 0);
        }

        private void WriteAll(byte[] buffer, int totalBytes)
        {
            m_Stream.Write(buffer, 0, totalBytes);
            m_Stream.Flush();
        }

        private bool ReadAll(byte[] buffer, int totalBytes)
        {
            int bytesLeft = totalBytes;
            while (bytesLeft > 0)
            {
                var bytesRead = m_Stream.Read(buffer, totalBytes - bytesLeft, bytesLeft);
                if (bytesRead == 0) return false;
                bytesLeft -= bytesRead;
            }
            return true;
        }

        public void Dispose()
        {
            m_Stream?.Dispose();
        }
    }
}