using System;

namespace RedGate.Ipc
{
    public interface IRpcClient : IDisposable
    {
        /// <summary>
        /// Regesters a concrete implementation of T to be used when remote parties create and use proxies of T.
        /// </summary>
        /// <typeparam name="T">Must be an interface.</typeparam>
        /// <param name="implementation">Must implement T.</param>
        void Register<T>(object implementation);

        /// <summary>
        /// The delegateFactory will be called once per type per connection
        /// and the result cached for that connection. It will not be disposed
        /// when the connection is disposed.
        /// </summary>
        void RegisterDi(Func<Type, object> delegateFactory);

        /// <summary>
        /// When a functionally identical interface has been declared in another assembly
        /// an alias can be registered which will redirect calls to the type of your choice.
        /// This can be useful if source files for interfaces are shared between projects and
        /// are built with more than one namespace.
        /// Aliases do not need to be a full assembly qualified name; matching is done with
        /// a StartsWith() match.
        /// </summary>
        /// <param name="assemblyQualifiedName">The alias, i.e. an assemblyQualifiedName that TypeGetType() would reject.</param>
        /// <param name="type">The local type to map the alias to.</param>
        void RegisterTypeAlias(string assemblyQualifiedName, Type type);

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
    }
}