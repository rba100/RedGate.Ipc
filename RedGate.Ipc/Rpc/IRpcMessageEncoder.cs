using RedGate.Ipc.Channel;

namespace RedGate.Ipc.Rpc
{
    internal interface IRpcMessageEncoder
    {
        ChannelMessage ToChannelMessage(RpcResponse response);
        ChannelMessage ToChannelMessage(RpcRequest request);
        ChannelMessage ToChannelMessage(RpcException exception);
        RpcResponse ToResponse(ChannelMessage channelMessage);
        RpcRequest ToRequest(ChannelMessage channelMessage);
        RpcException ToException(ChannelMessage message);
    }
}