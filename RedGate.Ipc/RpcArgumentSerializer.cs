using System;
using System.Linq;
using System.Reflection;

using RedGate.Ipc.Json;
using RedGate.Ipc.Proxy;
using RedGate.Ipc.Rpc;

namespace RedGate.Ipc
{
    internal static class RpcArgumentSerializer
    {
        private static readonly IJsonSerializer s_JsonSerializer = new TinyJsonSerializer();

        /// <summary>
        /// This method has two responsibilities. 'Dual responsiblity principle' - it'll be all the rage in a few years.
        ///  1) To convert a {MethodInfo,arguments[]} pair into an RpcRequest object. 
        ///  2) To pass the request object to a message broker correctly.
        /// </summary>
        public static object Call(this IRpcMessageBroker messageBroker, MethodInfo methodInfo, object[] arguments)
        {
            var interfaceName = methodInfo.DeclaringType?.AssemblyQualifiedName;
            if (interfaceName == null)
                throw new ContractMismatchException("Maybe loosen off on the reflection and generics a bit?");

            var request = new RpcRequest(
                Guid.NewGuid().ToString(),
                methodInfo.DeclaringType.AssemblyQualifiedName,
                methodInfo.GetRpcSignature(),
                arguments.Select(s_JsonSerializer.Serialize).ToArray());
            
            var async = methodInfo.GetCustomAttributes(typeof(ProxyNonBlockingAttribute), true).Any();

            if (async)
            {
                messageBroker.BeginRequest(request, null);
                return null;
            }

            var response = messageBroker.Send(request);
            if (methodInfo.ReturnType == typeof(void)) return null;
            return s_JsonSerializer.Deserialize(methodInfo.ReturnType, response.ReturnValue);
        }
    }
}