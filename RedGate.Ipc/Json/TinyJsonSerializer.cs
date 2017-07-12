using System;
using RedGate.Ipc.ImportedCode;

namespace RedGate.Ipc.Json
{
    internal class TinyJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializer m_JsonSerializer = new JsonSerializer(false);
        private readonly JsonDeserializer m_JsonDeserializer = new JsonDeserializer();

        public T Deserialize<T>(string json)
        {
            return m_JsonDeserializer.Deserialize<T>(json);
        }

        public string Serialize(object o)
        {
            return m_JsonSerializer.Serialize(o);
        }

        public object Deserialize(Type type, string json)
        {
            return m_JsonDeserializer.Deserialize(type, json);
        }
    }
}