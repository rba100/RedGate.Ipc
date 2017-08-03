using System;
using System.Collections.Generic;
using System.Linq;

namespace RedGate.Ipc.Rpc
{
    public class DelegateProvider : IDelegateProvider
    {
        private readonly List<Func<Type, object>> m_DependencyInjectors = new List<Func<Type, object>>();
        private readonly Dictionary<string, Type> m_TypeAliases = new Dictionary<string, Type>();

        public void AddDelegateFactory(Func<Type, object> delegateFactory)
        {
            m_DependencyInjectors.Add(delegateFactory);
        }

        public void AddTypeAlias(string typeNameStartsWith, Type type)
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
            foreach (var di in m_DependencyInjectors)
            {
                var obj = di(type);
                if (obj != null) return obj;
            }

            return null;
        }

        public T Get<T>()
        {
            return (T)Get(typeof(T));
        }
    }
}