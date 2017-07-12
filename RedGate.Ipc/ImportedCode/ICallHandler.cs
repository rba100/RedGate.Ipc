using System.Reflection;

namespace RedGate.Ipc.ImportedCode
{
    public interface ICallHandler
    {
        object HandleCall(MethodInfo methodInfo, object[] args);
    }
}
