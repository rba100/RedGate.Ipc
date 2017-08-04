using System;
using System.Collections.Generic;
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
        private List<IChannelStream> m_ActiveConnections = new List<IChannelStream>();

        public TcpEndpoint(
            int portNumber)
        {
            m_PortNumber = portNumber;
        }

        public void Start()
        {
            if (m_Disposed) throw new ObjectDisposedException(typeof(TcpEndpoint).FullName);
            if (m_Worker != null) return;
            m_Worker = new Thread(Worker)
            {
                Name = "TcpEndpoint Listener",
                IsBackground = true
            };
            m_Worker.Start();
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
                    var channelStream = new ChannelStream(new NetworkStream(socket));
                    m_ActiveConnections.Add(channelStream);
                    channelStream.Disconnected += () => m_ActiveConnections.Remove(channelStream);
                    ChannelConnected.Invoke(new ChannelConnectedEventArgs(
                        Guid.NewGuid().ToString(),
                        channelStream));
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

        public void Dispose()
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
            var activeConnections = m_ActiveConnections.ToArray();
            foreach (var activeConnection in activeConnections)
            {
                try
                {
                    activeConnection.Dispose();
                }
                catch
                {
                    //
                }
            }
        }
    }
}