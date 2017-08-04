using System;
using System.Threading;

namespace RedGate.Ipc.Channel
{
    internal class ChannelMessageReader : IDisposable
    {
        private readonly IChannelMessageStream m_ChannelMessageStream;
        private readonly IChannelMessageSerializer m_ChannelMessageSerializer;
        private readonly IChannelMessageHandler m_InboundHandler;

        private Thread m_Worker;

        private bool IsDisposed => m_Disposed != 0;
        private int m_Disposed;

        internal ChannelMessageReader(
            IChannelMessageStream channelMessageStream,
            IChannelMessageSerializer channelMessageSerializer,
            IChannelMessageHandler inboundHandler)
        {
            m_ChannelMessageStream = channelMessageStream;
            m_ChannelMessageSerializer = channelMessageSerializer;
            m_InboundHandler = inboundHandler;
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

                // Serious performance questions to be had here
                // ThreadPool can be very slow if the handler invokes duplex operations (which get handled by client's contested ThreadPool).
                // Manual threads result in less contention in certain cases but have unbounded overhead if tonnes of messages come it at once.

                // We can use this if we really need to:
                //var thread = new Thread(() => m_InboundHandler.Handle(channelMessage))
                //{
                //    IsBackground = true
                //};
                //thread.Start();

                // Otherwise this is fine for low concurrent service requests:
                //ThreadPool.QueueUserWorkItem(o => m_InboundHandler.Handle(channelMessage));
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