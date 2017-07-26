using System.IO.Pipes;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.NamedPipes
{
    public class NamedPipeEndpointClient : IEndpointClient
    {
        private readonly string m_PipeName;
        private readonly IRpcRequestHandler m_RpcRequestHandler = new RpcRequestHandler(new TinyJsonSerializer());

        public NamedPipeEndpointClient(string pipeName)
        {
            m_PipeName = pipeName;
        }

        public void Register<T>(object implementation)
        {
            m_RpcRequestHandler.Register<T>(implementation);
        }

        public void Dispose()
        {
            // No resources to dispose of
        }

        public IChannelStream Connect()
        {
            var client = new NamedPipeClientStream(".", m_PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            client.Connect(5000);
            return new SimpleStream(client);
        }
    }
}