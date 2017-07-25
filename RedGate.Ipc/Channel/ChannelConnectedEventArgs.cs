namespace RedGate.Ipc.Channel
{
    public sealed class ChannelConnectedEventArgs
    {
        public string ConnectionId { get; }
        public IChannelStream ChannelStream { get; }

        public ChannelConnectedEventArgs(string connectionId, IChannelStream channelStream)
        {
            ConnectionId = connectionId;
            ChannelStream = channelStream;
        }
    }
}