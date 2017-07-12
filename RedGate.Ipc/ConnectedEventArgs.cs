namespace RedGate.Ipc
{
    public sealed class ConnectedEventArgs
    {
        public IConnection Connection { get; }

        public ConnectedEventArgs(IConnection connection)
        {
            Connection = connection;
        }
    }
}