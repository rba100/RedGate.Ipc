using System;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc.NamedPipes
{
    public class NamedPipeEndpoint : IEndpoint
    {
        public event ChannelConnectedEventHandler ChannelConnected;

        private readonly string m_PipeName;
        private Thread m_Worker;
        private bool m_Disposed;

        public NamedPipeEndpoint(
            string pipeName)
        {
            m_PipeName = pipeName;
        }

        public void Start()
        {
            if (m_Worker == null)
            {
                m_Worker = new Thread(Worker);
                m_Worker.Start();
            }
        }

        private void Worker()
        {
            try
            {
                while (!m_Disposed)
                {
                    var listener = new NamedPipeServerStream(
                        m_PipeName,
                        PipeDirection.InOut,
                        254, // Max clients
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);
                    listener.WaitForConnection();
                    ChannelConnected?.Invoke(new ChannelConnectedEventArgs(Guid.NewGuid().ToString(), listener));
                }
            }
            catch (SocketException)
            {

            }
        }

        public void Stop()
        {
            m_Worker.Abort();
            m_Disposed = true;
        }
    }
}
