using System.IO;

namespace RedGate.Ipc.Channel
{
    public sealed class ChannelConnectedEventArgs
    {
        public string ConnectionId { get; }
        public Stream ChannelStream { get; }

        public ChannelConnectedEventArgs(string connectionId, Stream channelStream)
        {
            ConnectionId = connectionId;
            ChannelStream = channelStream;
        }
    }
}