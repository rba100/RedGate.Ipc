using System;
using System.Collections.Generic;

namespace RedGate.Ipc.Channel
{
    internal class ChannelMessagePipeline : IChannelMessageHandler
    {
        private readonly List<IChannelMessageHandler> m_Handlers;

        public ChannelMessagePipeline(IEnumerable<IChannelMessageHandler> handlers)
        {
            if (handlers == null) throw new ArgumentNullException(nameof(handlers));

            m_Handlers = new List<IChannelMessageHandler>(handlers);
        }

        public ChannelMessage Handle(ChannelMessage message)
        {
            var passedMessage = message;
            foreach (var handler in m_Handlers)
            {
                passedMessage = handler.Handle(passedMessage);
                if (passedMessage == null) break;
            }
            return passedMessage;
        }
    }
}