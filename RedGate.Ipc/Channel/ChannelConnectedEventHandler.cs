namespace RedGate.Ipc.Channel
{
    public delegate void ChannelConnectedEventHandler(ChannelConnectedEventArgs args);
    public delegate void ChannelDisconnectedEventHandler(ChannelDisconnectedEventArgs args);
    public delegate void ClientConnectedEventHandler(ConnectedEventArgs args);
    public delegate void ClientDisconnectedEventHandler(DisconnectedEventArgs args);
}