using System;

namespace RedGate.Ipc
{
    public interface IRpcClient : IDisposable
    {
        /// <summary>
        /// Creates an object impementing T that will forward calls to the connected server's implementation.
        /// The object will implement IDispose if it does not already, however calls Dispose() will no be
        /// remotely called. There is no need for consumers to call dispose on proxy objects.
        /// T must be an interface. The members on the object will throw <see cref="ChannelFaultedException"/> if
        /// the underlying connection fails.
        /// </summary>
        /// <param name="initialisation">An optional initialisation routine to run on the proxy before further commands are serviced.
        /// The routine may be called more than once, if the implementation has reconnection behaviour. The routine is guarenteed to run
        /// before calls to the proxy are serviced, but not necessarily immediately on reconnection.</param>
        /// <exception cref="ArgumentException">T was not an interface.</exception>
        T CreateProxy<T>(Action<T> initialisation = null);

        /// <summary>
        /// Creates an object impementing T that will forward calls to the connected server's implementation.
        /// The object will implement IDispose if it does not already, however calls Dispose() will no be
        /// remotely called. There is no need for consumers to call dispose on proxy objects.
        /// T must be an interface. The members on the object will throw TConnectionFailureExceptionType if
        /// the underlying connection fails.
        /// </summary>
        /// <param name="initialisation">An optional initialisation routine to run on the proxy before further commands are serviced.
        /// The routine may be called more than once, if the implementation has reconnection behaviour. The routine is guarenteed to run
        /// before calls to the proxy are serviced, but not necessarily immediately on reconnection.</param>
        /// <typeparam name="T">Must be an interface.</typeparam>
        /// <typeparam name="TConnectionFailureExceptionType">The exception type to be thrown in the event of a connection failure.
        /// The type must have a constructor that takes a string message argument.</typeparam>
        /// <exception cref="ArgumentException">T was not an interface or TConnectionFailureExceptionType did not have an appropriate constructor.</exception>
        T CreateProxy<T, TConnectionFailureExceptionType>(Action<T> initialisation = null) where TConnectionFailureExceptionType : Exception;
    }
}