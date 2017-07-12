using System;
using System.IO.Pipes;
using RedGate.Ipc.Json;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.NamedPipes
{
    public class NamedPipesRpcClient : IRpcClient
    {
        private readonly string m_PipeName;
        private readonly IRpcRequestHandler m_RpcRequestHandler = new RpcRequestHandler(new TinyJsonSerializer());
        private readonly RpcProxyFactory m_ProxyFactory = new RpcProxyFactory(new TinyJsonSerializer());
        private readonly IConnectionFactory m_ConnectionFactory = new ConnectionFactory(new RpcRequestHandler(new TinyJsonSerializer()));

        public NamedPipesRpcClient(string pipeName)
        {
            m_PipeName = pipeName;
        }

        public T CreateProxy<T>()
        {
            var client = new NamedPipeClientStream(".", m_PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            client.Connect(5000);
            client.ReadMode = PipeTransmissionMode.Byte;
            var connection = m_ConnectionFactory.Create(Guid.NewGuid().ToString(), client);
            return m_ProxyFactory.CreateProxy<T>(connection);
        }

        public void Register<T>(object implementation)
        {
            m_RpcRequestHandler.Register<T>(implementation);
        }
    }
}