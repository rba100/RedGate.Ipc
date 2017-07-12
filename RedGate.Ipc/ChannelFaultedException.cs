using System;

namespace RedGate.Ipc
{
    public class ChannelFaultedException : Exception
    {
        public ChannelFaultedException() : base("The connection has been closed or could not connect.")
        {
        }

        public ChannelFaultedException(string message) : base(message)
        {
        }

        public ChannelFaultedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}