using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RedGate.Ipc
{
    public class ClientConnectionAgent : IClientConnectionAgent
    {
        private readonly Func<IConnection> m_GetConnection;
        private readonly Action<IConnection> m_Initialisation;

        private const long c_DelayMs = 5000;
        private volatile bool m_Disposed;
        private readonly CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();
        private IConnection m_Connection;
        private readonly object m_ConnectionLock = new object();
        private readonly ManualResetEvent m_ConnectionWaitHandle = new ManualResetEvent(false);

        public ClientConnectionAgent(Func<IConnection> getConnection, Action<IConnection> initialisation)
        {
            m_GetConnection = getConnection;
            m_Initialisation = initialisation;

            AsyncReconnect();
        }

        public IConnection TryGetConnection(int timeoutMs = 0)
        {
            if (m_Disposed) throw new ObjectDisposedException(GetType().FullName);

            if (m_Connection?.IsConnected == false)
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
                    m_ConnectionWaitHandle.Reset();
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
