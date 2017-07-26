using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RedGate.Ipc.Json;

namespace RedGate.Ipc.Rpc
{
    public class RpcRequestHandler : IRpcRequestHandler
    {
        private readonly IJsonSerializer m_JsonSerializer;

        internal RpcRequestHandler(IJsonSerializer jsonSerializer)
        {
            m_JsonSerializer = jsonSerializer;
        }

        public RpcRequestHandler()
        {
            m_JsonSerializer = new TinyJsonSerializer();
        }

        private readonly Dictionary<string, object> m_Interfaces = new Dictionary<string, object>();

        public void Register<TInterface>(object implementation)
        {
            if (implementation.GetType().GetInterfaces().All(i => i != typeof(TInterface)))
            {
                throw new ArgumentException(
                    "Supplied implementation must implement the specified TInterface type.",
                    nameof(implementation));
            }
            m_Interfaces[typeof(TInterface).FullName] = implementation;
        }

        public RpcResponse Handle(RpcRequest request)
        {
            var handler = m_Interfaces[request.Interface];
            var methodType = handler.GetType().GetMethod(request.Method);

            var arguments =
                methodType.GetParameters()
                    .Select((p, i) => m_JsonSerializer.Deserialize(p.ParameterType, request.Arguments[i]))
                    .ToArray();
            try
            {
                var returnValue = methodType.Invoke(handler, arguments);


                if (methodType.ReturnType == typeof(void))
                {
                    return new RpcResponse(request.QueryId);
                }

                return new RpcResponse(request.QueryId, m_JsonSerializer.Serialize(returnValue));
            }
            catch (TargetInvocationException exception)
            {
                if (exception.InnerException != null)
                {
                    throw exception.InnerException;
                }
                throw;
            }
        }
    }
}