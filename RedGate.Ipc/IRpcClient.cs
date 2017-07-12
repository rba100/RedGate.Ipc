namespace RedGate.Ipc
{
    public interface IRpcClient
    {
        T CreateProxy<T>();
        void Register<T>(object implementation);
    }
}