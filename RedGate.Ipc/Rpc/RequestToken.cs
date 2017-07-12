using System;
using System.Threading;

namespace RedGate.Ipc.Rpc
{
    public class RequestToken : IDisposable
    {
        public ManualResetEvent Completed { get; } = new ManualResetEvent(false);

        public RpcResponse Response { get; set; }

        public void Dispose()
        {
            Completed?.Dispose();
        }
    }
}