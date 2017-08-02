using System;
using System.Collections.Generic;
using System.Linq;

namespace RedGate.Ipc.Rpc
{
    public class DelegateProvider : IDelegateProvider
    {
        private readonly List<Func<Type, object>> m_DependencyInjectors = new List<Func<Type, object>>();
        private readonly Dictionary<string, object> m_GlobalImplementations = new Dictionary<string, object>();
        private readonly Dictionary<string, Type> m_TypeAliases = new Dictionary<string, Type>();

        public void Register<TInterface>(object implementation)
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

        public void RegisterDi(Func<Type, object> delegateFactory)
        {
            m_DependencyInjectors.Add(delegateFactory);
        }

        public void RegisterAlias(string typeNameStartsWith, Type type)
        {
            m_TypeAliases[typeNameStartsWith] = type;
        }

        public object Get(string typeFullName)
        {
            var type =
                m_TypeAliases.Where(kvp => typeFullName.StartsWith(kvp.Key)).Select(kvp => kvp.Value).FirstOrDefault();
            type = type ?? Type.GetType(typeFullName);
            return Get(type);
        }

        public object Get(Type type)
        {
            object obj;
            // ReSharper disable once AssignNullToNotNullAttribute
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

        public T Get<T>()
        {
            return (T) Get(typeof(T));
        }
    }
}