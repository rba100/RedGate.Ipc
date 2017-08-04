using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using RedGate.Ipc.Json;

namespace RedGate.Ipc.Rpc
{
    public class RpcRequestHandler : IRpcRequestHandler
    {
        private readonly IDelegateProvider m_DelegateProvider;
        private readonly IJsonSerializer m_JsonSerializer;

        private readonly Dictionary<string, object> m_DelegateCache = new Dictionary<string, object>();

        public IConnection OwningConnection { get; set; }

        internal RpcRequestHandler(IDelegateProvider delegateProvider, IJsonSerializer jsonSerializer)
        {
            if (delegateProvider == null) throw new ArgumentNullException(nameof(delegateProvider));
            if (jsonSerializer == null) throw new ArgumentNullException(nameof(jsonSerializer));

            m_DelegateProvider = delegateProvider;
            m_JsonSerializer = jsonSerializer;
        }

        public RpcResponse Handle(RpcRequest request)
        {
            lock (m_DelegateCache)
            {
                if (!m_DelegateCache.ContainsKey(request.Interface))
                {
                    m_DelegateCache[request.Interface] = m_DelegateProvider.Get(request.Interface);
                }
            }

            var requestDelegate = m_DelegateCache[request.Interface];
            if (requestDelegate == null)
            {
                throw new ContractMismatchException($"The type '{request.Interface}' was not registered for RPC invocation.");
            }

            MethodInfo methodType;
            try
            {
                methodType =
                    requestDelegate.GetType()
                        .GetInterfaces()
                        .SelectMany(i => i.GetMethods())
                        .SingleOrDefault(m => m.GetRpcSignature() == request.MethodSignature);
            }
            catch(InvalidOperationException)
            {
                // This should never happen
                throw new ContractMismatchException($"Could not execute {request.MethodSignature} as multiple handlers were found.");
            }

            if (methodType == null)
                throw new ContractMismatchException($"No method with signature {request.MethodSignature} could be found on the service delegate {request.Interface}.");

            object[] arguments;
            try
            {
                arguments =
                    methodType.GetParameters()
                        .Select((p, i) => m_JsonSerializer.Deserialize(p.ParameterType, request.Arguments[i]))
                        .ToArray();
            }
            catch(Exception ex)
            {
                throw new ContractMismatchException($"The arguments for {request.MethodSignature} could not be deserialized.", ex);
            }

            try
            {
                var returnValue = methodType.Invoke(requestDelegate, arguments);

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