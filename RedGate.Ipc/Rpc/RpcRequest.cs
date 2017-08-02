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

        /// <summary>
        /// E.g. 
        /// int Add(int a, long b) => "Add_Int32_Int64"
        /// int Add() => "Add"
        /// Return type is not encoded as CLR doesn't have overrides that differ only by return type.
        /// Types are not fully qualified for extra compatability and/or bugs.
        /// </summary>
        public string MethodSignature { get; }

        /// <summary>
        /// The method arguments Json serialised.
        /// </summary>
        public string[] Arguments { get; }
    }

    internal static class RpcExtentionMethods
    {
        public static string GetRpcSignature(this MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters().Select(p => p.ParameterType.Name.ToString()).ToArray();
            return parameters.Any() ? $"{methodInfo.Name}_{String.Join("_", parameters)}" : methodInfo.Name;
        }
    }
}