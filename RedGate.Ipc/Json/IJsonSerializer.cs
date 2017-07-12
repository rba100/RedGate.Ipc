using System;

namespace RedGate.Ipc.Json
{
    internal interface IJsonSerializer
    {
        string Serialize(object o);
        T Deserialize<T>(string json);
        object Deserialize(Type type, string json);
    }
}
