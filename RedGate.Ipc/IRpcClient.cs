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
        /// <exception cref="ArgumentException">T was not an interface.</exception>
        T CreateProxy<T>();

        /// <summary>
        /// Creates an object impementing T that will forward calls to the connected server's implementation.
        /// The object will implement IDispose if it does not already, however calls Dispose() will no be
        /// remotely called. There is no need for consumers to call dispose on proxy objects.
        /// T must be an interface. The members on the object will throw TConnectionFailureExceptionType if
        /// the underlying connection fails.
        /// </summary>
        /// <typeparam name="T">Must be an interface.</typeparam>
        /// <typeparam name="TConnectionFailureExceptionType">The exception type to be thrown in the event of a connection failure.
        /// The type must have a constructor that takes a string message argument.</typeparam>
        /// <exception cref="ArgumentException">T was not an interface or TConnectionFailureExceptionType did not have an appropriate constructor.</exception>
        T CreateProxy<T, TConnectionFailureExceptionType>() where TConnectionFailureExceptionType : Exception;

        /// <summary>
        /// The number of times the underlying connection has been established.
        /// </summary>
        long ConnectionRefreshCount { get; }
    }
}