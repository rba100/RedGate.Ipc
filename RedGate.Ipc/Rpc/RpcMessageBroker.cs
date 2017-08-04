using System;
using System.Collections.Generic;
using System.Threading;

namespace RedGate.Ipc.Rpc
{
    public delegate void DisconnectedEventHandler();

    public class RpcMessageBroker : IRpcMessageBroker
    {
        private readonly Dictionary<string, RequestToken> m_PendingQueries
            = new Dictionary<string, RequestToken>();

        private readonly IRpcMessageWriter m_RpcMessageWriter;

        private readonly ManualResetEvent m_CancellationToken = new ManualResetEvent(false);

        internal RpcMessageBroker(
            IRpcMessageWriter messageWriter)
        {
            m_RpcMessageWriter = messageWriter;
        }

        public RpcResponse Send(RpcRequest request)
        {
            using (var token = new RequestToken())
            {
                BeginRequest(request, token);
                try
                {
                    var waitValue = WaitHandle.WaitAny(new[]
                    {
                        token.Completed,
                        m_CancellationToken
                    }, TimeSpan.FromSeconds(15));

                    m_PendingQueries.Remove(request.QueryId);

                    if (waitValue == 0)
                    {
                        if (token.Response != null) return token.Response;
                        if (token.Exception != null) throw token.Exception;
                    }

                    if (waitValue == WaitHandle.WaitTimeout) throw new ChannelFaultedException("Connection timed out.");
                }
                catch (ObjectDisposedException)
                {
                    
                }

                throw new ChannelFaultedException("The connection was closed before the response was received.");
            }
        }

        public void BeginRequest(RpcRequest request, RequestToken requestToken)
        {
            m_PendingQueries[request.QueryId] = requestToken;
            m_RpcMessageWriter.Write(request);
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
                m_PendingQueries.Remove(response.QueryId);
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
                m_PendingQueries.Remove(message.QueryId);
            }
        }

        public void Dispose()
        {
            m_CancellationToken.Set();
            try
            {
                m_CancellationToken.Close();
            }
            catch
            {
                //
            }
        }
    }
}
