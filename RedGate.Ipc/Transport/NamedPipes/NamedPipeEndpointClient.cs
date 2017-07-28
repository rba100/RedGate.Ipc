using System.IO.Pipes;

using RedGate.Ipc.Channel;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.NamedPipes
{
    public class NamedPipeEndpointClient : IEndpointClient
    {
        private readonly string m_PipeName;

        public NamedPipeEndpointClient(string pipeName)
        {
            m_PipeName = pipeName;
        }

        public void Dispose()
        {
            // No resources to dispose of
        }

        public IChannelStream Connect()
        {
            var client = new NamedPipeClientStream(".", m_PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            client.Connect(5000);
            return new ChannelStream(client);
        }
    }
}