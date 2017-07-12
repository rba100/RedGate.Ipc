namespace RedGate.Ipc.Rpc
{
    internal class RpcRequestBinding
    {
        public string QueryId { get; set; }
        public string Interface { get; set; }
        public string Method { get; set; }
        public string[] Arguments { get; set; }
    }
}
