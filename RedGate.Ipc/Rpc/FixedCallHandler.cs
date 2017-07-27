using System;
using System.Linq;
using System.Reflection;
using RedGate.Ipc.ImportedCode;
using RedGate.Ipc.Json;

namespace RedGate.Ipc.Rpc
{
    internal class FixedCallHandler<T> : ICallHandler
    {
        private readonly IConnection m_Connection;
        private readonly IJsonSerializer m_JsonSerializer;

        public FixedCallHandler(IConnection connection, IJsonSerializer jsonSerializer)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (jsonSerializer == null) throw new ArgumentNullException(nameof(jsonSerializer));

            m_Connection = connection;
            m_JsonSerializer = jsonSerializer;
        }

        public object HandleCall(MethodInfo methodInfo, object[] args)
        {
            // This should not happen
            if (methodInfo.Name == "Dispose")
            {
                return null;
            }

            var serialisedArgs = args.Select(a => m_JsonSerializer.Serialize(a)).ToArray();
            var request = new RpcRequest(Guid.NewGuid().ToString(), typeof(T).AssemblyQualifiedName, methodInfo.Name, serialisedArgs);
            var response = m_Connection.RpcMessageBroker.Send(request);
            if (methodInfo.ReturnType == typeof(void)) return null;
            return m_JsonSerializer.Deserialize(methodInfo.ReturnType, response.ReturnValue);
        }

        public void HandleDispose()
        {
            m_Connection.Dispose();
        }
    }
}