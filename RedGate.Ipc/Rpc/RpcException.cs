using System;

namespace RedGate.Ipc.Rpc
{
    public class RpcException
    {
        public string QueryId { get; }
        public Exception Exception { get; }

        public RpcException(string queryId, Exception exception)
        {
            QueryId = queryId;
            Exception = exception;
        }
    }
}
