using System;
using System.Collections.Generic;
using System.Linq;

namespace RedGate.Ipc.Rpc
{
    public interface ITypeResolver
    {
        void RegisterGlobal<TInterface>(object implementation);

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
        /// <param name="typeNameStartsWith">The alias, i.e. an assemblyQualifiedName that TypeGetType() would reject.</param>
        /// <param name="type">The local type to map the alias to.</param>
        void RegisterTypeAlias(string typeNameStartsWith, Type type);

        object Resolve(string typeFullName);
        object Resolve(Type type);
        T Resolve<T>();
    }

    public class TypeResolver : ITypeResolver
    {
        private readonly List<Func<Type, object>> m_DependencyInjectors = new List<Func<Type, object>>();
        private readonly Dictionary<string, object> m_GlobalImplementations = new Dictionary<string, object>();
        private readonly Dictionary<string, Type> m_TypeAliases = new Dictionary<string, Type>();

        public void RegisterGlobal<TInterface>(object implementation)
        {
            if (implementation.GetType().GetInterfaces().All(i => i != typeof(TInterface)))
            {
                throw new ArgumentException(
                    "Supplied implementation must implement the specified TInterface type.",
                    nameof(implementation));
            }
            m_GlobalImplementations[typeof(TInterface).AssemblyQualifiedName] = implementation;
        }

        /// <summary>
        /// The delegateFactory will be called once per type per connection
        /// and the result cached for that connection. It will not be disposed
        /// when the connection is disposed.
        /// </summary>
        public void RegisterDi(Func<Type, object> delegateFactory)
        {
            m_DependencyInjectors.Add(delegateFactory);
        }

        public void RegisterTypeAlias(string typeNameStartsWith, Type type)
        {
            m_TypeAliases[typeNameStartsWith] = type;
        }

        public object Resolve(string typeFullName)
        {
            var type =
                m_TypeAliases.Where(kvp => typeFullName.StartsWith(kvp.Key)).Select(kvp => kvp.Value).FirstOrDefault();
            type = type ?? Type.GetType(typeFullName);
            return Resolve(type);
        }

        public object Resolve(Type type)
        {
            object obj;
            if (m_GlobalImplementations.TryGetValue(type.AssemblyQualifiedName, out obj))
            {
                return obj;
            }

            foreach (var di in m_DependencyInjectors)
            {
                obj = di(type);
                if (obj != null) return obj;
            }

            return null;
        }

        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }
    }
}
