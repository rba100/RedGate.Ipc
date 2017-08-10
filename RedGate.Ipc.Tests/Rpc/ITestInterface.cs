using RedGate.Ipc.Rpc;

namespace RedGate.Ipc.Tests.Rpc
{
    public interface ITestInterface
    {
        [RpcNonBlocking]
        void AsyncVoidCall();

        void VoidCall();

        void Polymorphic(int a);
        void Polymorphic(string a);
    }
}