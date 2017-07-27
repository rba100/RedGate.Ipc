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

        object Resolve(string typeFullName);
        object Resolve(Type type);
        T Resolve<T>();
    }

    public class TypeResolver : ITypeResolver
    {
        private readonly List<Func<Type, object>> m_DependencyInjectors = new List<Func<Type, object>>();
        private readonly Dictionary<string, object> m_GlobalImplementations = new Dictionary<string, object>();

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

        public object Resolve(string typeFullName)
        {
            return Resolve(Type.GetType(typeFullName));
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
