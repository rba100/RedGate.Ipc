using System;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IRpcClient
    {
        T CreateProxy<T>();
        void Register<T>(object implementation);
    }

    public class RpcClient : IRpcClient
    {
        private readonly IChannelStreamClientProvider m_ChannelStreamClientProvider;
        private readonly IClientConnectionAgent m_ClientConnectionAgent;

        public RpcClient(IChannelStreamClientProvider channelStreamClientProvider, IClientConnectionAgent clientConnectionAgent)
        {
            if (channelStreamClientProvider == null)
                throw new ArgumentNullException(nameof(channelStreamClientProvider));

            m_ChannelStreamClientProvider = channelStreamClientProvider;
            m_ClientConnectionAgent = clientConnectionAgent;
        }

        public T CreateProxy<T>()
        {
            throw new System.NotImplementedException();
        }

        public void Register<T>(object implementation)
        {
            throw new System.NotImplementedException();
        }
    }
}