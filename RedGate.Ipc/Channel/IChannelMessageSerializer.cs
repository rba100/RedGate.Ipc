namespace RedGate.Ipc.Channel
{
    internal interface IChannelMessageSerializer
    {
        ChannelMessage FromBytes(byte[] bytes);
        byte[] ToBytes(ChannelMessage channelMessage);
    }
}