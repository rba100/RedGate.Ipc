using System;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;

using RedGate.Ipc.Channel;

namespace RedGate.Ipc.NamedPipes
{
    public class NamedPipeEndpoint : IEndpoint
    {
        public event ChannelConnectedEventHandler ChannelConnected = delegate { };

        private readonly string m_PipeName;
        private Thread m_Worker;
        private bool m_Disposed;
        private NamedPipeServerStream m_CurrentListener;

        public NamedPipeEndpoint(
            string pipeName)
        {
            m_PipeName = pipeName;
        }

        public void Start()
        {
            if (m_Disposed) throw new InvalidOperationException($"{nameof(NamedPipeEndpoint)} does not support restarting.");
            if (m_Worker == null)
            {
                m_Worker = new Thread(Worker)
                {
                    Name = "NamedPipeEndpoint Listener"
                };
                m_Worker.Start();
            }
        }

        private void Worker()
        {
            try
            {
                while (!m_Disposed)
                {
                    m_CurrentListener = new NamedPipeServerStream(
                        m_PipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);
                    m_CurrentListener.WaitForConnection();
                    ChannelConnected(new ChannelConnectedEventArgs(Guid.NewGuid().ToString(), new ChannelStream(m_CurrentListener)));
                }
            }
            catch (SocketException)
            {

            }
        }

        public void Stop()
        {
            m_Disposed = true;
            m_CurrentListener?.Dispose();
            m_Worker.Abort();
        }
    }
}
