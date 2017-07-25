using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc.Tcp
{
    public class TcpEndpoint : IEndpoint
    {
        private readonly int m_PortNumber;
        private TcpListener Listener;
        private Thread m_Worker;
        private bool m_Disposed;

        public TcpEndpoint(
            int portNumber)
        {
            m_PortNumber = portNumber;
        }

        public void Start()
        {
            if (m_Disposed) throw new InvalidOperationException("TcpEndpoint does not support restarting.");
            if (m_Worker != null) return;
            m_Worker = new Thread(Worker);
            m_Worker.Start();
        }

        public void Stop()
        {
            m_Disposed = true;
            try
            {
                Listener?.Stop();
            }
            catch
            {
                //
            }
            m_Worker?.Abort();
        }

        private void Worker()
        {
            Listener = new TcpListener(IPAddress.Any, m_PortNumber);
            Listener.Start();
            try
            {
                while (!m_Disposed)
                {
                    var socket = Listener.AcceptSocket();
                    var stream = new NetworkStream(socket);
                    ChannelConnected?.Invoke(new ChannelConnectedEventArgs(Guid.NewGuid().ToString(), new SimpleStream(stream)));
                }
            }
            catch (SocketException)
            {

            }
        }

        public event ChannelConnectedEventHandler ChannelConnected;
    }
}