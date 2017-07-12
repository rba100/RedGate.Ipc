using System;
using System.Linq;
using System.Reflection;
using RedGate.Ipc.ImportedCode;
using RedGate.Ipc.Json;

namespace RedGate.Ipc.Rpc
{
    internal class RpcCallHandler<T> : ICallHandler
    {
        private readonly IRpcMessageBroker m_MessageBroker;
        private readonly IJsonSerializer m_JsonSerializer;

        public RpcCallHandler(IRpcMessageBroker messageBroker, IJsonSerializer jsonSerializer)
        {
            if (messageBroker == null) throw new ArgumentNullException(nameof(messageBroker));
            if (jsonSerializer == null) throw new ArgumentNullException(nameof(jsonSerializer));

            m_MessageBroker = messageBroker;
            m_JsonSerializer = jsonSerializer;
        }

        public object HandleCall(MethodInfo methodInfo, object[] args)
        {
            var serialisedArgs = args.Select(a => m_JsonSerializer.Serialize(a)).ToArray();
            var request = new RpcRequest(Guid.NewGuid().ToString(), typeof(T).Name, methodInfo.Name, serialisedArgs);
            var response = m_MessageBroker.Send(request);
            if (methodInfo.ReturnType == typeof(void)) return null;
            return m_JsonSerializer.Deserialize(methodInfo.ReturnType, response.ReturnValue);
        }
    }
}