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
        private TcpListener m_Listener;
        private Thread m_Worker;
        private bool m_Disposed;

        public TcpEndpoint(
            int portNumber)
        {
            m_PortNumber = portNumber;
        }

        public void Start()
        {
            if (m_Disposed) throw new InvalidOperationException($"{nameof(TcpEndpoint)} does not support restarting.");
            if (m_Worker != null) return;
            m_Worker = new Thread(Worker)
            {
                Name = "TcpEndpoint Listener"
            };
            m_Worker.Start();
        }

        public void Stop()
        {
            m_Disposed = true;
            try
            {
                m_Listener?.Stop();
            }
            catch
            {
                //
            }
            m_Worker?.Abort();
        }

        private void Worker()
        {
            m_Listener = new TcpListener(IPAddress.Any, m_PortNumber);
            m_Listener.Start();
            try
            {
                while (!m_Disposed)
                {
                    var socket = m_Listener.AcceptSocket();
                    var stream = new NetworkStream(socket);
                    ChannelConnected.Invoke(new ChannelConnectedEventArgs(
                        Guid.NewGuid().ToString(),
                        new ChannelStream(stream)));
                }
            }
            catch (ObjectDisposedException)
            {

            }
            catch (SocketException)
            {

            }
        }

        public event ChannelConnectedEventHandler ChannelConnected = delegate { };
    }
}