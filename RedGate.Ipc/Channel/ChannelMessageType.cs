namespace RedGate.Ipc.Channel
{
    internal enum ChannelMessageType : int
    {
        Heartbeat = 0,
        RpcRequest = 1,
        RpcResponse = 2
    }
}