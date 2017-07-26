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
        private readonly Action<IConnection> m_Initialisation;

        // Constants
        private const long c_DelayMs = 5000;

        // State variables
        private volatile bool m_Disposed;
        private IConnection m_Connection;

        // Synchronisation objects
        private readonly object m_ConnectionLock = new object();
        private readonly ManualResetEvent m_ConnectionWaitHandle = new ManualResetEvent(false);
        private readonly CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();

        public ReliableConnectionAgent(Func<IConnection> getConnection, Action<IConnection> initialisation)
        {
            m_GetConnection = getConnection;
            m_Initialisation = initialisation;

            AsyncReconnect();
        }

        public IConnection TryGetConnection(int timeoutMs)
        {
            if (m_Disposed) throw new ObjectDisposedException(GetType().FullName);

            if (m_Connection?.IsConnected != true)
            {
                AsyncReconnect();
            }

            if (timeoutMs > 0 && !m_ConnectionWaitHandle.WaitOne(timeoutMs))
            {
                return null;
            }
            return m_Connection?.IsConnected == true ? m_Connection : null;
        }

        private void ConnectionOnDisconnected(DisconnectedEventArgs args)
        {
            lock (m_ConnectionLock)
            {
                if (m_Connection == args.Connection)
                {
                    if(!m_Disposed) m_ConnectionWaitHandle.Reset();
                    m_Connection = null;
                    AsyncReconnect();
                }
            }
            args.Connection.Dispose();
        }

        private void AsyncReconnect()
        {
            if (m_Disposed) return;
            lock (m_ConnectionLock)
            {
                if (m_Connection?.IsConnected == true) return;
                m_ConnectionWaitHandle.Reset();
                m_Connection = null;
                Task.Factory.StartNew(
                    ReconnectLoop,
                    TaskCreationOptions.LongRunning);
            }
        }

        private void ReconnectLoop()
        {
            var stopwatch = new Stopwatch();
            do
            {
                if (m_Disposed) return;
                stopwatch.Restart();
                try
                {
                    m_ConnectionWaitHandle.Reset();
                    var connection = m_GetConnection();
                    m_Initialisation?.Invoke(connection);
                    m_Connection = connection;
                    connection.Disconnected += ConnectionOnDisconnected;
                    m_ConnectionWaitHandle.Set();
                }
                catch (Exception)
                {
                    //
                }
                var remaingDelay = (int)(stopwatch.ElapsedMilliseconds - c_DelayMs);
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

            } while (m_Connection?.IsConnected != true);
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
