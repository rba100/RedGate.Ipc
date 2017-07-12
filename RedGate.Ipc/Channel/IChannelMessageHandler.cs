namespace RedGate.Ipc.Channel
{
    internal interface IChannelMessageHandler
    {
        ChannelMessage Handle(ChannelMessage message);
    }
}