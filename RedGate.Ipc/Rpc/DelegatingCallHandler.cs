using System;
using System.Reflection;
using RedGate.Ipc.ImportedCode;

namespace RedGate.Ipc.Rpc
{
    internal class DelegatingCallHandler : ICallHandler
    {
        private readonly Func<MethodInfo, object[], object> m_Handler;
        private readonly Action m_DisposeHandler;

        public DelegatingCallHandler(
            Func<MethodInfo, object[], object> handler,
            Action disposeHandler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (disposeHandler == null) throw new ArgumentNullException(nameof(disposeHandler));

            m_Handler = handler;
            m_DisposeHandler = disposeHandler;
        }

        public object HandleCall(MethodInfo methodInfo, object[] args)
        {
            return m_Handler(methodInfo, args);
        }

        public void HandleDispose()
        {
            m_DisposeHandler();
        }
    }
}