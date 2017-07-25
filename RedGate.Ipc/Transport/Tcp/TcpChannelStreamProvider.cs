using System.Net.Sockets;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc.Tcp
{
    public class TcpChannelStreamProvider : IChannelStreamProvider
    {
        private readonly int m_PortNumber;
        private readonly string m_HostName;

        public TcpChannelStreamProvider(int portNumber, string hostName)
        {
            m_PortNumber = portNumber;
            m_HostName = hostName;
        }
        public void Dispose()
        {
            // No resources to dispose of
        }

        public IChannelStream Connect()
        {
            var client = new TcpClient();
            client.Connect(m_HostName, m_PortNumber);
            return new ChannelStreamCustomDispose(client.GetStream(), client.Close);
        }
    }
}