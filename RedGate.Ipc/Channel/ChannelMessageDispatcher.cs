using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.Channel
{
    internal class ChannelMessageDispatcher : IChannelMessageDispatcher
    {
        private readonly IMessageStream m_MessageStream;
        private readonly IChannelMessageSerializer m_ChannelMessageSerializer;
        private readonly IChannelMessageMessagePipeline m_ChannelMessageMessagePipeline;

        private Thread m_Worker;
        private bool m_Disposed;

        internal ChannelMessageDispatcher(
            IMessageStream messageStream,
            IChannelMessageSerializer channelMessageSerializer,
            IChannelMessageMessagePipeline channelMessageMessagePipeline)
        {
            m_MessageStream = messageStream;
            m_ChannelMessageSerializer = channelMessageSerializer;
            m_ChannelMessageMessagePipeline = channelMessageMessagePipeline;
        }

        public void Send(ChannelMessage channelMessage)
        {
            m_MessageStream.Write(m_ChannelMessageSerializer.ToBytes(channelMessage));
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
                throw new InvalidOperationException($"{nameof(ChannelMessageDispatcher)} does not support restarting.");
            }
        }

        public void Stop()
        {
            m_Disposed = true;
            m_MessageStream.Dispose();
            m_Worker?.Abort();
        }

        private void Worker()
        {
            while (!m_Disposed)
            {
                ChannelMessage message;
                try
                {
                    var bytes = m_MessageStream.Read();
                    if (bytes == null)
                    {
                        break;
                    }
                    message = m_ChannelMessageSerializer.FromBytes(bytes);
                    m_ChannelMessageMessagePipeline.Handle(message);
                }
                catch (ChannelFaultedException)
                {
                    break;
                }
                catch (IOException)
                {
                    break;
                }
                catch (ObjectDisposedException)
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
            try
            {
                m_Disposed = true;
                m_MessageStream.Dispose();
            }
            catch
            {
                //
            }
        }
    }
}