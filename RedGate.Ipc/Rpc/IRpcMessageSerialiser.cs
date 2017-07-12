namespace RedGate.Ipc.Rpc
{
    internal interface IRpcMessageSerialiser
    {
        string SerialiseResponse(RpcResponse response);
        string SerialiseRequest(RpcRequest request);
        RpcResponse DeserialiseResponse(string json);
        RpcRequest DeserialiseRequest(string json);
    }
}