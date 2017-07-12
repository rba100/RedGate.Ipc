namespace RedGate.Ipc.Channel
{
    internal interface IChannelMessageMessageHandler
    {
        ChannelMessage Handle(ChannelMessage message);
    }
}