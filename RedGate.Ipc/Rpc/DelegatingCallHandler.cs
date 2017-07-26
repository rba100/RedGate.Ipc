using System;
using System.Reflection;

using RedGate.Ipc.ImportedCode;

namespace RedGate.Ipc.Rpc
{
    internal class DelegatingCallHandler : ICallHandler
    {
        private readonly Func<MethodInfo, object[], object> m_Handler;
        private readonly Action m_DisposeHandler;
        private readonly Type m_ConnectionFailureExceptionType;

        public DelegatingCallHandler(
            Func<MethodInfo, object[], object> handler,
            Action disposeHandler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (disposeHandler == null) throw new ArgumentNullException(nameof(disposeHandler));

            m_Handler = handler;
            m_DisposeHandler = disposeHandler;
        }

        public DelegatingCallHandler(
            Func<MethodInfo, object[], object> handler,
            Action disposeHandler,
            Type exceptionTypeConnectionFailure) : this(handler, disposeHandler)
        {
            if (exceptionTypeConnectionFailure == null)
                throw new ArgumentNullException(nameof(exceptionTypeConnectionFailure));

            if (!exceptionTypeConnectionFailure.IsSubclassOf(typeof(Exception)))
            {
                throw new ArgumentException(
                    "The type must be a subclass of Exception to be used as an exception type override.",
                    nameof(exceptionTypeConnectionFailure));
            }

            if (exceptionTypeConnectionFailure.GetConstructor(new Type[] { typeof(String) }) == null)
            {
                throw new ArgumentException(
                       $"{exceptionTypeConnectionFailure.Name} must have a constructor that takes a string message to be used as an exception type override.",
                       nameof(exceptionTypeConnectionFailure));
            }

            m_ConnectionFailureExceptionType = exceptionTypeConnectionFailure;
        }

        public object HandleCall(MethodInfo methodInfo, object[] args)
        {
            try
            {
                return m_Handler(methodInfo, args);
            }
            catch (ChannelFaultedException ex)
            {
                if (m_ConnectionFailureExceptionType == null) throw;
                throw (Exception)Activator.CreateInstance(m_ConnectionFailureExceptionType, new object[] { ex.Message });
            }
        }

        public void HandleDispose()
        {
            m_DisposeHandler();
        }
    }
}