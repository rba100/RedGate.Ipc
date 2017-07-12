namespace RedGate.Ipc.Rpc
{
    internal interface IRpcMessageWriter
    {
        void Write(RpcRequest request);
        void Write(RpcResponse response);
    }
}