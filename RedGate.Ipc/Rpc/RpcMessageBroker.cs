using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RedGate.Ipc.Rpc
{
    public delegate void DisconnectedEventHandler();

    internal class RpcMessageBroker : IRpcMessageBroker, IDisposable
    {
        public event DisconnectedEventHandler Disconnected;

        private readonly ConcurrentDictionary<string, RequestToken> m_PendingQueries
            = new ConcurrentDictionary<string, RequestToken>();

        private readonly IRpcMessageWriter m_RpcMessageWriter;
        private readonly IRpcRequestHandler m_RpcRequestHandler;

        private readonly CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();

        internal RpcMessageBroker(
            IRpcMessageWriter messageWriter,
            IRpcRequestHandler rpcRequestHandler)
        {
            m_RpcMessageWriter = messageWriter;
            m_RpcRequestHandler = rpcRequestHandler;
        }

        public RpcResponse Send(RpcRequest request)
        {
            using (var token = new RequestToken())
            {
                BeginRequest(request, token);

                var waitValue = WaitHandle.WaitAny(new[]
                {
                    token.Completed,
                    m_CancellationTokenSource.Token.WaitHandle
                }, TimeSpan.FromSeconds(15));

                if (waitValue == 0)
                {
                    if(token.Response != null) return token.Response;
                    throw token.Exception;
                }

                RequestToken dummy;
                m_PendingQueries.TryRemove(request.QueryId, out dummy);
                if (waitValue == WaitHandle.WaitTimeout) throw new Exception("Client timed out.");
                throw new ChannelFaultedException();
            }
        }

        public void BeginRequest(RpcRequest request, RequestToken requestToken)
        {
            m_PendingQueries[request.QueryId] = requestToken;
            try
            {
                m_RpcMessageWriter.Write(request);
            }
            catch (ChannelFaultedException)
            {
                Disconnected?.Invoke();
            }
        }

        public void HandleInbound(RpcRequest request)
        {
            RpcResponse response = null;
            try
            {
                response = m_RpcRequestHandler.Handle(request);
                
            }
            catch (Exception exception)
            {
                var rpcException = new RpcException(request.QueryId, exception);
                m_RpcMessageWriter.Write(rpcException);
            }
            if (response != null) m_RpcMessageWriter.Write(response);
        }

        public void HandleInbound(RpcResponse response)
        {
            RequestToken requestToken;
            if (m_PendingQueries.TryGetValue(response.QueryId, out requestToken))
            {
                requestToken.Response = response;
                try
                {
                    requestToken.Completed.Set();
                }
                catch (ObjectDisposedException) { }
                m_PendingQueries.TryRemove(response.QueryId, out requestToken);
            }
        }

        public void HandleInbound(RpcException message)
        {
            RequestToken requestToken;
            if (m_PendingQueries.TryGetValue(message.QueryId, out requestToken))
            {
                requestToken.Exception = message.Exception;
                try
                {
                    requestToken.Completed.Set();
                }
                catch (ObjectDisposedException) { }
                m_PendingQueries.TryRemove(message.QueryId, out requestToken);
            }
        }

        public void Dispose()
        {
            m_CancellationTokenSource.Cancel();
        }
    }
}
