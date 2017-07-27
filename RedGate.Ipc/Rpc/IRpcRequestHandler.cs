namespace RedGate.Ipc.Rpc
{
    public interface IRpcRequestHandler
    {
        RpcResponse Handle(RpcRequest request);
    }
}
