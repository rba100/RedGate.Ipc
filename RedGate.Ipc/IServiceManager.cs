using System;
using RedGate.Ipc.Channel;

namespace RedGate.Ipc
{
    public interface IServiceManager
    {
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

        void AddEndpoint(IEndpoint endpoint);
        void Start();
        void Stop();

        event ClientConnectedEventHandler ClientConnected;
    }


}
