﻿using System;
using System.Diagnostics;
using System.Threading;

namespace RedGate.Ipc
{
    public class ReconnectingConnectionProvider : IReconnectingConnectionProvider
    {
        // Dependencies
        private readonly Func<IConnection> m_GetConnection;

        // Settings
        public int RetryDelayMs { get; set; } = 5000;

        // State variables
        private volatile bool m_Disposed;
        private volatile IConnection m_Connection;
        private long m_ConnectionRefreshCount;

        // Synchronisation objects
        private readonly object m_ConnectionLock = new object();
        private readonly ManualResetEvent m_ConnectionWaitHandle = new ManualResetEvent(false);
        private readonly ManualResetEvent m_CancellationToken = new ManualResetEvent(false);
        private readonly WaitHandle[] m_TryGetConnectionWaitHandles;

        public event EventHandler Reconnected = delegate { };

        public ReconnectingConnectionProvider(Func<IConnection> getConnection)
        {
            m_GetConnection = getConnection;
            m_TryGetConnectionWaitHandles = new WaitHandle[]
            {
                m_ConnectionWaitHandle,
                m_CancellationToken
            };
            AsyncReconnect();
        }

        public IConnection TryGetConnection(int timeoutMs)
        {
            if (m_Disposed) throw new ObjectDisposedException(GetType().FullName);

            // ReSharper disable once InconsistentlySynchronizedField
            if (timeoutMs > 0)
            {
                var waitResult = WaitHandle.WaitAny(m_TryGetConnectionWaitHandles, timeoutMs);
                if (waitResult == 0) return m_Connection;
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
                    if (m_Disposed) return;
                    try
                    {
                        m_ConnectionWaitHandle.Reset();
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    m_Connection = null;
                    AsyncReconnect();
                }
            }
        }

        private void AsyncReconnect()
        {
            var reconnectThread = new Thread(ReconnectLoop)
            {
                IsBackground = true
            };
            reconnectThread.Start();
        }

        private void ReconnectLoop()
        {
            var stopwatch = new Stopwatch();

            while (!m_Disposed)
            {
                stopwatch.Reset();
                stopwatch.Start();
                try
                {
                    m_ConnectionWaitHandle.Reset();
                    var connection = m_GetConnection();
                    lock (m_ConnectionLock)
                    {
                        if (m_Disposed)
                        {
                            connection.Dispose();
                            return;
                        }
                        m_Connection = connection;
                        Interlocked.Increment(ref m_ConnectionRefreshCount);
                        connection.Disconnected += ConnectionOnDisconnected;
                    }
                    if (m_Connection?.IsConnected == true)
                    {
                        Reconnected(this, EventArgs.Empty);
                        m_ConnectionWaitHandle.Set();
                    }
                    return;
                }
                catch (Exception)
                {
                    //
                }
                var remaingDelay = (int)(RetryDelayMs - stopwatch.ElapsedMilliseconds);
                if (remaingDelay > 0)
                {
                    try
                    {
                        if(m_CancellationToken.WaitOne(remaingDelay)) return;
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
            if (!m_Disposed)
            {
                m_Disposed = true;

                try
                {
                    m_CancellationToken.Close();
                }
                catch
                {
                    //
                }

                try
                {
                    m_ConnectionWaitHandle.Close();
                }
                catch
                {
                    //
                }

                m_Connection?.Dispose();
            }
        }
    }
}
