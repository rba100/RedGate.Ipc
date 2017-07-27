using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using RedGate.Ipc.Json;

namespace RedGate.Ipc.Rpc
{
    public class RpcRequestHandler : IRpcRequestHandler
    {
        private readonly ITypeResolver m_TypeResolver;
        private readonly IJsonSerializer m_JsonSerializer;

        private readonly Dictionary<string, object> m_DelegateCache = new Dictionary<string, object>();

        internal RpcRequestHandler(ITypeResolver typeResolver, IJsonSerializer jsonSerializer)
        {
            if (typeResolver == null) throw new ArgumentNullException(nameof(typeResolver));
            if (jsonSerializer == null) throw new ArgumentNullException(nameof(jsonSerializer));

            m_TypeResolver = typeResolver;
            m_JsonSerializer = jsonSerializer;
        }

        public RpcResponse Handle(RpcRequest request)
        {
            lock (m_DelegateCache)
            {
                if (!m_DelegateCache.ContainsKey(request.Interface))
                {
                    m_DelegateCache[request.Interface] = m_TypeResolver.Resolve(request.Interface);
                }
            }
            
            var handler = m_DelegateCache[request.Interface];
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