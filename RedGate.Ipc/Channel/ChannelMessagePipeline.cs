using System.Collections.Generic;

namespace RedGate.Ipc.Channel
{
    internal class ChannelMessagePipeline : IChannelMessageMessagePipeline
    {
        private readonly List<IChannelMessageMessageHandler> m_Handlers;

        public ChannelMessagePipeline(IEnumerable<IChannelMessageMessageHandler> handlers)
        {
            m_Handlers = new List<IChannelMessageMessageHandler>(handlers);
        }

        public void Handle(ChannelMessage message)
        {
            ChannelMessage passedMessage = message;
            foreach (var handler in m_Handlers)
            {
                passedMessage = handler.Handle(passedMessage);
                if (passedMessage == null) break;
            }
            // TODO: Log unhandled message?
        }
    }
}