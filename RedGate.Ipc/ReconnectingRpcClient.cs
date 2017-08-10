using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using RedGate.Ipc.Channel;
using RedGate.Ipc.Proxy;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    public class ReconnectingRpcClient : IRpcClient
    {
        private static readonly ProxyFactory s_ProxyFactory = new ProxyFactory();

        private readonly IDelegateCollection m_DelegateCollection;
        private readonly IReconnectingConnectionProvider m_ConnectionProvider;
        private readonly ITaskLauncher m_TaskLauncher;

        private readonly Dictionary<object, ProxyState> m_ProxyState = new Dictionary<object, ProxyState>();

        private bool m_IsDisposed;

        public int ConnectionTimeoutMs { get; set; } = 6000;

        internal ReconnectingRpcClient(
            IDelegateCollection delegateCollection,
            IReconnectingConnectionProvider connectionProvider,
            ITaskLauncher taskLauncher)
        {
            if (delegateCollection == null) throw new ArgumentNullException(nameof(delegateCollection));
            if (connectionProvider == null) throw new ArgumentNullException(nameof(connectionProvider));
            if (taskLauncher == null) throw new ArgumentNullException(nameof(taskLauncher));

            m_DelegateCollection = delegateCollection;
            m_ConnectionProvider = connectionProvider;
            m_TaskLauncher = taskLauncher;

            m_ConnectionProvider.Reconnected += ConnectionProviderOnReconnected;
        }

        private void ConnectionProviderOnReconnected(object sender, EventArgs eventArgs)
        {
            m_TaskLauncher.StartShortTask(() =>
            {
                try
                {
                    var connection = m_ConnectionProvider.TryGetConnection(ConnectionTimeoutMs);
                    if (connection == null) return; // Another event will be raised.
                    var proxies = m_ProxyState.Keys.ToArray();

                    foreach (var proxy in proxies)
                    {
                        CheckRunInit(connection.ConnectionId, proxy);
                    }
                }
                catch
                {
                    // Failure will be reported when proxy method is called.
                }
            });
        }

        public T CreateProxy<T>(Action<T> initialisation = null)
        {
            ICallHandler callHandler = new DelegatingCallHandler(HandleCall, ProxyDisposed);
            var proxy = s_ProxyFactory.Create<T>(callHandler);
            AddProxyState(proxy, initialisation == null ? (Action<object>)null : o => initialisation((T)o));
            return proxy;
        }

        public T CreateProxy<T, TConnectionFailureExceptionType>(Action<T> initialisation = null) where TConnectionFailureExceptionType : Exception
        {
            ICallHandler callHandler = new DelegatingCallHandler(HandleCall, ProxyDisposed, typeof(TConnectionFailureExceptionType));
            var proxy = s_ProxyFactory.Create<T>(callHandler);
            AddProxyState(proxy, initialisation == null ? (Action<object>)null : o => initialisation((T)o));
            return proxy;
        }

        public void AddDelegateFactory(Func<Type, object> delegateFactory)
        {
            m_DelegateCollection.DependencyInjectors.Add(delegateFactory);
        }

        public void AddTypeAlias(string assemblyQualifiedName, Type type)
        {
            m_DelegateCollection.TypeAliases.Add(assemblyQualifiedName, type);
        }

        private object HandleCall(object sender, MethodInfo methodInfo, object[] args)
        {
            if (m_IsDisposed) throw new ObjectDisposedException(typeof(ReconnectingRpcClient).FullName, $"The underlying {nameof(ReconnectingRpcClient)} was disposed.");

            var connection = m_ConnectionProvider.TryGetConnection(ConnectionTimeoutMs);
            if (connection == null) throw new ChannelFaultedException("Unable to connect.");

            CheckRunInit(connection.ConnectionId, sender);

            return connection.RpcMessageBroker.Call(methodInfo, args);
        }

        private void ProxyDisposed(object sender)
        {
            RemoveProxyState(sender);
        }

        public void Dispose()
        {
            m_IsDisposed = true;
            m_ConnectionProvider?.Dispose();
        }

        private void AddProxyState(object proxy, Action<object> init)
        {
            var state = new ProxyState
            {
                ConnectionId = string.Empty,
                InitialisationRoutine = init,
                InitLock = new object()
            };
            m_ProxyState[proxy] = state;
        }

        private void RemoveProxyState(object proxy)
        {
            m_ProxyState.Remove(proxy);
        }

        private void CheckRunInit(string connectionId, object proxy)
        {
            if (!m_ProxyState.TryGetValue(proxy, out ProxyState state)) return;
            if (state.InitialisationRoutine == null) return;
            bool lockTaken = false;
            try
            {
                lockTaken = Monitor.TryEnter(state.InitLock, ConnectionTimeoutMs);
                if (!lockTaken) throw new ChannelFaultedException(
                        "Timed out waiting for connection. Initialisation routine took too long to complete.");

                if (state.ConnectionId == connectionId) return;
                state.ConnectionId = connectionId;

                // This will cause recursion back here and Monitor.TryEnter will succeed because same thread
                // but connectionId has been updated so 'if (state.ConnectionId == connectionId) return' will happen.
                state.InitialisationRoutine(proxy);
            }
            catch (ObjectDisposedException)
            {
                throw new ObjectDisposedException(typeof(ReconnectingRpcClient).FullName, $"The RPC client was disposed.");
            }
            finally
            {
                try
                {
                    if (lockTaken) Monitor.Exit(state.InitLock);
                }
                catch (Exception)
                {
                    //
                }
            }
        }

        private class ProxyState
        {
            public string ConnectionId;
            public Action<object> InitialisationRoutine;
            public object InitLock;
        }
    }
}