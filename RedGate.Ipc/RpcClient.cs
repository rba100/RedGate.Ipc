﻿using System;
using System.Linq;
using System.Reflection;

using RedGate.Ipc.ImportedCode;
using RedGate.Ipc.Json;
using RedGate.Ipc.NamedPipes;
using RedGate.Ipc.Rpc;
using RedGate.Ipc.Tcp;

namespace RedGate.Ipc
{
    public class RpcClient : IRpcClient
    {
        private readonly IDelegateResolver m_DelegateResolver;
        private readonly IReliableConnectionAgent m_ReliableConnectionAgent;
        private readonly IJsonSerializer m_JsonSerializer;

        private static readonly ProxyFactory s_ProxyFactory = new ProxyFactory();
        private bool m_IsDisposed;

        public int ConnectionTimeoutMs { get; set; } = 6000;

        internal RpcClient(
            IDelegateResolver delegateResolver,
            IReliableConnectionAgent reliableConnectionAgent)
        {
            if (delegateResolver == null) throw new ArgumentNullException(nameof(delegateResolver));
            if (reliableConnectionAgent == null) throw new ArgumentNullException(nameof(reliableConnectionAgent));

            m_DelegateResolver = delegateResolver;
            m_ReliableConnectionAgent = reliableConnectionAgent;
            m_JsonSerializer = new TinyJsonSerializer();
        }

        public static IRpcClient CreateNamedPipeClient(string pipeName)
        {
            var namedPipesClient = new NamedPipeEndpointClient(pipeName);
            var typeResolver = new DelegateResolver();
            var connectionFactory = new ConnectionFactory(typeResolver);
            var clientAgent = new ReliableConnectionAgent(() => connectionFactory.Create(Guid.NewGuid().ToString(), namedPipesClient.Connect()));
            return new RpcClient(typeResolver, clientAgent);
        }

        public static IRpcClient CreateTcpClient(string hostname, int portNumber)
        {
            var tcpProvider = new TcpEndpointClient(portNumber, hostname);
            var typeResolver = new DelegateResolver();
            var connectionFactory = new ConnectionFactory(typeResolver);
            var clientAgent = new ReliableConnectionAgent(() => connectionFactory.Create(Guid.NewGuid().ToString(), tcpProvider.Connect()));
            return new RpcClient(typeResolver, clientAgent);
        }

        public T CreateProxy<T>()
        {
            ICallHandler callHandler = new DelegatingCallHandler(HandleCallReconnectOnFailure, ProxyDisposed);
            return s_ProxyFactory.Create<T>(callHandler);
        }

        public T CreateProxy<T, TConnectionFailureExceptionType>() where TConnectionFailureExceptionType : Exception
        {
            ICallHandler callHandler = new DelegatingCallHandler(HandleCallReconnectOnFailure, ProxyDisposed, typeof(TConnectionFailureExceptionType));
            return s_ProxyFactory.Create<T>(callHandler);
        }

        public void Register<T>(object implementation)
        {
            m_DelegateResolver.Register<T>(implementation);
        }

        public void RegisterDi(Func<Type, object> delegateFactory)
        {
            m_DelegateResolver.RegisterDi(delegateFactory);
        }

        public void RegisterTypeAlias(string assemblyQualifiedName, Type type)
        {
            m_DelegateResolver.RegisterAlias(assemblyQualifiedName, type);
        }

        private object HandleCallReconnectOnFailure(MethodInfo methodInfo, object[] args)
        {
            if (m_IsDisposed) throw new ObjectDisposedException(typeof(RpcClient).FullName, $"The underlying {nameof(RpcClient)} was disposed.");
            var request = new RpcRequest(
                    Guid.NewGuid().ToString(),
                    methodInfo.DeclaringType.AssemblyQualifiedName,
                    methodInfo.Name, args.Select(m_JsonSerializer.Serialize).ToArray());
            var connection = m_ReliableConnectionAgent.TryGetConnection(ConnectionTimeoutMs);
            if (m_IsDisposed) throw new ObjectDisposedException(typeof(RpcClient).FullName, $"The underlying {nameof(RpcClient)} was disposed.");
            if (connection == null) throw new ChannelFaultedException("Timed out trying to connect");
            var response = connection.RpcMessageBroker.Send(request);
            if (methodInfo.ReturnType == typeof(void)) return null;
            return m_JsonSerializer.Deserialize(methodInfo.ReturnType, response.ReturnValue);
        }

        private void ProxyDisposed()
        {
            // Ignore
        }

        public void Dispose()
        {
            m_IsDisposed = true;
            m_ReliableConnectionAgent?.Dispose();
        }
    }
}