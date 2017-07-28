using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RedGate.Ipc
{
    public class ReliableConnectionAgent : IReliableConnectionAgent
    {
        // Dependencies
        private readonly Func<IConnection> m_GetConnection;

        // Constants
        private const long c_RetryDelayMs = 5000;

        // State variables
        private volatile bool m_Disposed;
        private volatile IConnection m_Connection;

        // Synchronisation objects
        private readonly object m_ConnectionLock = new object();
        private readonly ManualResetEvent m_ConnectionWaitHandle = new ManualResetEvent(false);
        private readonly CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();

        public ReliableConnectionAgent(Func<IConnection> getConnection)
        {
            m_GetConnection = getConnection;

            AsyncReconnect();
        }

        public IConnection TryGetConnection(int timeoutMs)
        {
            if (m_Disposed) throw new ObjectDisposedException(GetType().FullName);

            // ReSharper disable once InconsistentlySynchronizedField
            if (timeoutMs > 0 && !m_ConnectionWaitHandle.WaitOne(timeoutMs))
            {
                return null;
            }
            if (m_Disposed) throw new ObjectDisposedException(GetType().FullName);
            return m_Connection;
        }

        private void ConnectionOnDisconnected(DisconnectedEventArgs args)
        {
            lock (m_ConnectionLock)
            {
                if (m_Connection == args.Connection)
                {
                    m_ConnectionWaitHandle.Reset();
                    m_Connection = null;
                    AsyncReconnect();
                }
            }
        }

        private void AsyncReconnect()
        {
            Task.Factory.StartNew(
                ReconnectLoop,
                TaskCreationOptions.LongRunning);
        }

        private void ReconnectLoop()
        {
            var stopwatch = new Stopwatch();

            while (!m_Disposed)
            {
                stopwatch.Restart();
                try
                {
                    m_ConnectionWaitHandle.Reset();
                    var connection = m_GetConnection();
                    lock(m_ConnectionLock)
                    {
                        m_Connection = connection;
                        connection.Disconnected += ConnectionOnDisconnected;
                    }
                    if(m_Connection?.IsConnected == true) m_ConnectionWaitHandle.Set();
                    return;
                }
                catch (Exception)
                {
                    //
                }
                var remaingDelay = (int)(c_RetryDelayMs - stopwatch.ElapsedMilliseconds);
                if (remaingDelay > 0)
                {
                    try
                    {
                        m_CancellationTokenSource.Token.WaitHandle.WaitOne(remaingDelay);
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                }
            }
        }

        public void Dispose()
        {
            m_Disposed = true;
            m_CancellationTokenSource.Cancel();
            m_ConnectionWaitHandle.Dispose();
            m_Connection?.Dispose();
        }
    }
}
