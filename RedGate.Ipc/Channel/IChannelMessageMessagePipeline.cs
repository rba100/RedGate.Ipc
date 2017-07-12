namespace RedGate.Ipc.Channel
{
    internal interface IChannelMessageMessagePipeline
    {
        void Handle(ChannelMessage message);
    }
}
