namespace RedGate.Ipc.Rpc
{
    public interface IRpcMessageWriter
    {
        void Write(RpcRequest request);
        void Write(RpcResponse response);
        void Write(RpcException exception);
    }
}