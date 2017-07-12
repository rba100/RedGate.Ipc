namespace RedGate.Ipc.Channel
{
    internal interface IChannelMessageWriter
    {
        void Write(ChannelMessage channelMessage);
    }
}