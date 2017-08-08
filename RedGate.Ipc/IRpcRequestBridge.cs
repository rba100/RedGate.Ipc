using System.Reflection;

namespace RedGate.Ipc
{
    internal interface IRpcRequestBridge
    {
        object Call(MethodInfo methodInfo, object[] args);
    }
}