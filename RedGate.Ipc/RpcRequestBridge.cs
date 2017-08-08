using System;
using System.Linq;
using System.Reflection;

using RedGate.Ipc.Json;
using RedGate.Ipc.Proxy;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    /// <summary>
    /// This class has two responsibilities. 'Dual responsiblity principle' - it'll be all the rage in a few years.
    ///  1) To convert a {MethodInfo,arguments[]} pair into an RpcRequest object. 
    ///  2) To pass the request object to a message broker correctly
    /// </summary>
    internal class RpcRequestBridge : IRpcRequestBridge
    {
        private static readonly IJsonSerializer s_JsonSerializer = new TinyJsonSerializer();
        private readonly IRpcMessageBroker m_MessageBroker;

        public RpcRequestBridge(IRpcMessageBroker messageBroker)
        {
            m_MessageBroker = messageBroker;
        }

        public object Call(MethodInfo methodInfo, object[] args)
        {
            var request = new RpcRequest(
                Guid.NewGuid().ToString(),
                methodInfo.DeclaringType.AssemblyQualifiedName,
                methodInfo.GetRpcSignature(),
                args.Select(s_JsonSerializer.Serialize).ToArray());
            
            var async = methodInfo.GetCustomAttributes(typeof(ProxyNonBlockingAttribute), true).Any();

            if (async)
            {
                m_MessageBroker.BeginRequest(request, null);
                return null;
            }

            var response = m_MessageBroker.Send(request);
            if (methodInfo.ReturnType == typeof(void)) return null;
            return s_JsonSerializer.Deserialize(methodInfo.ReturnType, response.ReturnValue);
        }
    }
}