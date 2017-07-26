using System;
using System.Threading;
using System.Threading.Tasks;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.Channel
{
    internal class ChannelMessageReader : IChannelMessageDispatcher
    {
        private readonly IChannelMessageStream m_ChannelMessageStream;
        private readonly IChannelMessageSerializer m_ChannelMessageSerializer;
        private readonly IChannelMessageHandler m_InboundHandler;

        private Thread m_Worker;
        private bool m_Disposed;

        internal ChannelMessageReader(
            IChannelMessageStream channelMessageStream,
            IChannelMessageSerializer channelMessageSerializer,
            IChannelMessageHandler inboundHandler)
        {
            m_ChannelMessageStream = channelMessageStream;
            m_ChannelMessageSerializer = channelMessageSerializer;
            m_InboundHandler = inboundHandler;
        }

        public void Send(ChannelMessage channelMessage)
        {
            m_ChannelMessageStream.Write(m_ChannelMessageSerializer.ToBytes(channelMessage));
        }

        public void Start()
        {
            if (m_Worker == null)
            {
                m_Worker = new Thread(Worker);
                m_Worker.Start();
            }
            else
            {
                throw new InvalidOperationException($"{nameof(ChannelMessageReader)} does not support restarting.");
            }
        }

        public void Stop()
        {
            m_Disposed = true;
            m_ChannelMessageStream.Dispose();
            m_Worker?.Abort();
        }

        private void Worker()
        {
            while (!m_Disposed)
            {
                try
                {
                    var bytes = m_ChannelMessageStream.Read();
                    if (bytes == null)
                    {
                        break;
                    }
                    var message = m_ChannelMessageSerializer.FromBytes(bytes);
                    Task.Run(() => m_InboundHandler.Handle(message));
                }
                catch (Exception)
                {
                    break;
                }
            }
            Disconnected?.Invoke();
            return;
        }

        public event DisconnectedEventHandler Disconnected;

        public void Dispose()
        {
            m_Disposed = true;
            m_ChannelMessageStream.Dispose();
        }
    }
}