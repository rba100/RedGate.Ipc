namespace RedGate.Ipc.Rpc
{
    public class RpcResponse
    {
        public string QueryId { get; }
        public string ReturnValue { get; }

        public RpcResponse(string queryId)
        {
            QueryId = queryId;
        }

        public RpcResponse(string queryId, string returnValue)
        {
            QueryId = queryId;
            ReturnValue = returnValue;
        }
    }
}