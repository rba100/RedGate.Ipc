namespace RedGate.Ipc.Channel
{
    internal static class ChannelMessageExtentionMethods
    {
        internal static bool IsMessageType(this ChannelMessage message, ChannelMessageType type)
        {
            return message.HandlerCode == (int) type;
        }

        internal static ChannelMessageType Type(this ChannelMessage message)
        {
            return (ChannelMessageType) message.HandlerCode;
        }
    }
}
