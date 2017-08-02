namespace RedGate.Ipc.Rpc
{
    internal class RpcRequestBinding
    {
        public string QueryId { get; set; }
        public string Interface { get; set; }
        public string MethodSignature { get; set; }
        public string[] Arguments { get; set; }
    }
}
