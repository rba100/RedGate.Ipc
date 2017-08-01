using System;
using System.Collections.Generic;
using System.Linq;

namespace RedGate.Ipc.Rpc
{
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
            // ReSharper disable once AssignNullToNotNullAttribute
            // typeof(t) should not return a type that does not have AQN
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