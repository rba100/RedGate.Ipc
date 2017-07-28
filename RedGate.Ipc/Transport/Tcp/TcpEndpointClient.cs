using System.Net.Sockets;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc.Tcp
{
    public class TcpEndpointClient : IEndpointClient
    {
        private readonly int m_PortNumber;
        private readonly string m_HostName;

        public TcpEndpointClient(int portNumber, string hostName)
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
            var stream = client.GetStream();
            return new ChannelStream(
                stream, () =>
                {
                    client.Close();
                    stream.Dispose();
                });
        }
    }
}