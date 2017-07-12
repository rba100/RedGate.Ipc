namespace RedGate.Ipc
{
    public sealed class DisconnectedEventArgs
    {
        public IConnection Connection { get; }

        public DisconnectedEventArgs(IConnection connection)
        {
            Connection = connection;
        }
    }
}