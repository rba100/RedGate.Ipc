using System;
using System.Runtime.Serialization;

namespace RedGate.Ipc
{
    [Serializable]
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

        protected ChannelFaultedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}