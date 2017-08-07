namespace RedGate.Ipc.Tests.Rpc
{
    public interface ITestInterface
    {
        [ProxyNonBlocking]
        void AsyncVoidCall();

        void VoidCall();

        void Polymorphic(int a);
        void Polymorphic(string a);
    }
}