namespace RedGate.Ipc.Tests.Rpc
{
    public interface ITestInterface
    {
        void VoidCall();

        void Polymorphic(int a);
        void Polymorphic(string a);
    }
}