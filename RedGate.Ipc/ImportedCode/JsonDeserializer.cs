using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace RedGate.Ipc.ImportedCode
{
    internal class JsonDeserializer
    {
        private static readonly JsonParser _parser = new JsonParser();

        public T Deserialize<T>(string json)
        {
            return (T)Deserialize(typeof(T), json);
        }

        public object Deserialize(Type type, string json)
        {
            var jsonValue = _parser.Parse(json);
            return Deserialize(type, jsonValue);
        }

        public T Deserialize<T>(StreamReader jsonTextStream)
        {
            return (T)Deserialize(typeof(T), jsonTextStream);
        }

        public object Deserialize(Type type, StreamReader jsonTextStream)
        {
            var jsonValue = _parser.Parse(jsonTextStream);
            return Deserialize(type, jsonValue);
        }

        private object Deserialize(Type type, JsonValue jsonValue)
        {
            if (jsonValue is JsonString) return DeserializeString(type, (JsonString)jsonValue);
            if (jsonValue is JsonObject) return DeserializeObject(type, (JsonObject)jsonValue);
            if (jsonValue is JsonArray)  return DeserializeArray(type, (JsonArray)jsonValue);
            if (jsonValue is JsonNumber) return DeserializeNumber(type, (JsonNumber)jsonValue);
            if (jsonValue is JsonNull)   return DeserializeNull(type);
            if (jsonValue is JsonTrue)   return DeserializeBoolean(type, true);
            if (jsonValue is JsonFalse)  return DeserializeBoolean(type, false);

            throw new JsonException($"No deserializer for {jsonValue.GetType().Name}");
        }

        private object DeserializeNumber(Type type, JsonNumber jsonNumber)
        {
            try
            {
                if (type == typeof(int)) return int.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(long)) return long.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(decimal)) return decimal.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(float)) return float.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(double)) return double.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(uint)) return uint.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(ulong)) return ulong.Parse(jsonNumber.StringRepresentation);
                if (type == typeof(byte)) return byte.Parse(jsonNumber.StringRepresentation);
            }
            catch (FormatException)
            {
                throw new JsonException($"Malformed {type.Name}: '{jsonNumber.StringRepresentation}'");
            }

            // Fallback
            var tc = TypeDescriptor.GetConverter(type);
            if(tc.CanConvertFrom(typeof(string))) return tc.ConvertFromString(jsonNumber.StringRepresentation);
            
            throw new JsonException($"Could not map {jsonNumber.StringRepresentation} to {type.Name}");
        }

        private object DeserializeArray(Type type, JsonArray jsonArray)
        {
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var length = jsonArray.Items.Count;
                var array = Array.CreateInstance(elementType, length);
                for (var i = 0; i < length; i++)
                {
                    array.SetValue(Deserialize(elementType, jsonArray.Items[i]), i);
                }
                return array;
            }
            throw new JsonException($"Could not map json array to {type.Name}");
        }

        private object DeserializeObject(Type type, JsonObject jsonObject)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) return CreateDictionary(type, jsonObject);
            if (type.IsClass) return CreateClass(type, jsonObject);

            throw new JsonException($"Could not map {jsonObject.GetType().Name} to {type.Name}");
        }

        private object DeserializeNull(Type type)
        {
            if (!type.IsValueType) return null;
            throw new JsonException($"Could not map JsonNull to a value type '{type.Name}'");
        }

        private object DeserializeBoolean(Type type, bool value)
        {
            if (type == typeof(bool)) return value;
            throw new JsonException($"Could not map '{value}' to {type.Name}");
        }

        private object CreateDictionary(Type type, JsonObject jsonValue)
        {
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];
            var add = type.GetMethod("Add", new[] { keyType, valueType });
            var dictionary = Activator.CreateInstance(type);
            foreach (var member in jsonValue.Members)
            {
                var key = DeserializeFromString(keyType, member.Name);
                var value = Deserialize(valueType, member.Value);
                add.Invoke(dictionary, new[] { key, value });
            }
            return dictionary;
        }

        private object DeserializeFromString(Type type, string str)
        {
            if (type == typeof(string)) return str;

            if (type == typeof(DateTime)) return DateTime.Parse(str, null, System.Globalization.DateTimeStyles.RoundtripKind);

            var tc = TypeDescriptor.GetConverter(type);
            if (tc.CanConvertFrom(typeof(string))) return tc.ConvertFromString(str);

            throw new JsonException($"Could not map string to {type.Name}");
        }

        private object DeserializeString(Type type, JsonString jsonString)
        {
            return DeserializeFromString(type, jsonString.Value);
        }

        private object CreateClass(Type type, JsonObject jsonObject)
        {
            var paramaterlessConstructor = type.GetConstructor(Type.EmptyTypes);
            if (paramaterlessConstructor == null) throw new JsonException($"No paramaterless constructor found for {type.Name}");
            var obj = Activator.CreateInstance(type);

            foreach (var member in jsonObject.Members)
            {
                var exactProperty = type.GetProperty(member.Name, BindingFlags.Instance | BindingFlags.Public);
                if (exactProperty != null)
                {
                    var propertyValue = Deserialize(exactProperty.PropertyType, member.Value);
                    exactProperty.SetValue(obj, propertyValue, null);
                    continue;
                }

                var property = type.GetProperty(member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (property != null)
                {
                    var propertyValue = Deserialize(property.PropertyType, member.Value);
                    property.SetValue(obj, propertyValue, null);
                    continue;
                }

                var exactField = type.GetField(member.Name, BindingFlags.Instance | BindingFlags.Public);
                if (exactField != null)
                {
                    var fieldValue = Deserialize(exactField.FieldType, member.Value);
                    exactField.SetValue(obj, fieldValue);
                    continue;
                }

                var field = type.GetField(member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var fieldValue = Deserialize(field.FieldType, member.Value);
                    field.SetValue(obj, fieldValue);
                }
            }

            return obj;
        }
    }
}
