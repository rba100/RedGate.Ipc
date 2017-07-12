namespace RedGate.Ipc.Rpc
{
    public class RpcRequest
    {
        public RpcRequest(string queryId, string interfaceName, string method, string[] arguments)
        {
            QueryId = queryId;
            Interface = interfaceName;
            Method = method;
            Arguments = arguments;
        }

        public string QueryId { get; }
        public string Interface { get; }
        public string Method { get; }
        public string[] Arguments { get; }
    }
}