using System;
using System.Threading;

namespace RedGate.Ipc.Channel
{
    internal class ChannelMessageReader : IDisposable
    {
        private readonly IChannelMessageStream m_ChannelMessageStream;
        private readonly IChannelMessageSerializer m_ChannelMessageSerializer;
        private readonly IChannelMessageHandler m_InboundHandler;
        private readonly ITaskLauncher m_TaskLauncher;

        private Thread m_Worker;

        private bool IsDisposed => m_Disposed != 0;
        private int m_Disposed;

        internal ChannelMessageReader(
            IChannelMessageStream channelMessageStream,
            IChannelMessageSerializer channelMessageSerializer,
            IChannelMessageHandler inboundHandler,
            ITaskLauncher taskLauncher)
        {
            if (channelMessageStream == null) throw new ArgumentNullException(nameof(channelMessageStream));
            if (channelMessageSerializer == null) throw new ArgumentNullException(nameof(channelMessageSerializer));
            if (inboundHandler == null) throw new ArgumentNullException(nameof(inboundHandler));
            if (taskLauncher == null) throw new ArgumentNullException(nameof(taskLauncher));

            m_ChannelMessageStream = channelMessageStream;
            m_ChannelMessageSerializer = channelMessageSerializer;
            m_InboundHandler = inboundHandler;
            m_TaskLauncher = taskLauncher;
        }

        public void Start()
        {
            if (m_Worker != null) return;
            m_Worker = new Thread(Worker)
            {
                Name = "ChannelMessageReader",
                IsBackground = true
            };
            m_Worker.Start();
        }

        private void Worker()
        {
            while (!IsDisposed)
            {
                ChannelMessage channelMessage = null;

                try
                {
                    var bytes = m_ChannelMessageStream.Read();
                    channelMessage = m_ChannelMessageSerializer.FromBytes(bytes);
                }
                catch (ObjectDisposedException)
                {
                    Dispose();
                    return;
                }
                catch (ChannelFaultedException)
                {
                    Dispose();
                    return;
                }

                m_TaskLauncher.StartShortTask(() => m_InboundHandler.Handle(channelMessage));
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_Disposed, 1) != 0)
            {
                return;
            }
            m_ChannelMessageStream.Dispose();
        }
    }
}