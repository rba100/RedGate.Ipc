using System;

namespace RedGate.Ipc
{
    public interface IDelegateRegistrar
    {
        /// <summary>
        /// The delegateFactory will be called once per type per connection
        /// and the result cached for that connection. It will not be disposed
        /// when the connection is disposed.
        /// </summary>
        void AddDelegateFactory(Func<Type, object> delegateFactory);

        /// <summary>
        /// When a functionally identical interface has been declared in another assembly
        /// an alias can be registered which will redirect calls to the type of your choice.
        /// This can be useful if source files for interfaces are shared between projects and
        /// are built with more than one namespace.
        /// Aliases do not need to be a full assembly qualified name; matching is done with
        /// a StartsWith() match.
        /// </summary>
        /// <param name="alias">The alias, i.e. all or part of an assemblyQualifiedName that Type.GetType() would reject.</param>
        /// <param name="interfaceType">The interface type to map the alias to.</param>
        void AddTypeAlias(string alias, Type interfaceType);
    }
}