using System;

namespace RedGate.Ipc.Rpc
{
    internal class RpcExceptionBinding
    {
        public string QueryId { get; set; }
        public Guid ExceptionTypeClsid { get; set; }
        public string Exception { get; set; }
    }
}