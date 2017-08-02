using System;
using System.Linq;
using System.Reflection;

namespace RedGate.Ipc.Rpc
{
    public class RpcRequest
    {
        public RpcRequest(string queryId, string interfaceName, string methodSignature, string[] arguments)
        {
            QueryId = queryId;
            Interface = interfaceName;
            MethodSignature = methodSignature;
            Arguments = arguments;
        }

        public string QueryId { get; }
        public string Interface { get; }
        public string MethodSignature { get; }
        public string[] Arguments { get; }
    }

    internal static class RpcExtentionMethods
    {
        public static string GetRpcSignature(this MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters().Select(p => p.ParameterType.ToString()).ToArray();
            if (parameters.Any())
            {
                var thing = $"{methodInfo.Name}_{String.Join("_", parameters)}";
                return thing;
            }

            return methodInfo.Name;
        }
    }
}