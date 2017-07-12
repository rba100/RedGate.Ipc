/*
Copyright 2016, 2017 Red Gate Software Ltd.

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
"AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
language governing permissions and limitations under the License.

In addition, the copyright holders grant permission to reproduce and distribute copies of this software or derivative
works thereof in any medium, with or without modifications, in Object form (as defined by the License), without
satisfying the requirements of section 4a of the License. In practice, this means that you are free to include this
library in binary releases of your own software without having to also include this notice, a copy of the Licence, or
any other copyright attribution.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace /***$rootnamespace$.***/ RedGate.Ipc.ImportedCode
{
    /// <summary>
    /// Lightweight, thread-safe serializer class used to convert objects to JSON.
    /// </summary>
    /// <remarks>
    /// Consumers are advised to create a single instance of this class, appropriately configured via the constructor
    /// parameters, and reuse it to convert objects to JSON using <see cref="Serialize(object)"/>,
    /// <see cref="Serialize(object,TextWriter)"/> or <see cref="Serialize(object,StringBuilder)"/>.
    /// </remarks>
    internal sealed class JsonSerializer
    {
        private readonly IDictionary<Type, Action<object, Action<string>, Action<char>>> _basicHandlers;
        private readonly bool _indented;
        private readonly string _keyValueSeparator;
        private string[] _indents = new string[0];

        /// <summary>
        /// Creates a new serializer instance.
        /// </summary>
        /// <param name="indented">If <c>true</c>, the generated JSON will be formatted on multiple lines with
        /// indentation, to improve human readability. If <c>false</c>, the generated JSON will be compact, with no
        /// line-breaks or unnecessary whitespace.</param>
        public JsonSerializer(bool indented)
        {
            _basicHandlers =
                new Dictionary<Type, Action<object, Action<string>, Action<char>>>
                {
                    [typeof(bool)] = SerializeBool,
                    [typeof(byte)] = SerializeWithToString,
                    [typeof(sbyte)] = SerializeWithToString,
                    [typeof(short)] = SerializeWithToString,
                    [typeof(ushort)] = SerializeWithToString,
                    [typeof(int)] = SerializeWithToString,
                    [typeof(uint)] = SerializeWithToString,
                    [typeof(long)] = SerializeWithToString,
                    [typeof(ulong)] = SerializeWithToString,
                    [typeof(float)] = SerializeFloat,
                    [typeof(double)] = SerializeDouble,
                    [typeof(Decimal)] = SerializeDecimal,
                    [typeof(char)] = SerializeChar,
                    [typeof(string)] = SerializeString,
                    [typeof(DateTime)] = SerializeDateTime,
                    [typeof(DateTimeOffset)] = SerializeDateTimeOffset,
                    [typeof(Guid)] = SerializeGuid,
                };
            _indented = indented;
            _keyValueSeparator = indented ? ": " : ":";
        }

        /// <summary>
        /// Serializes the specified object to its equivalent JSON representation.
        /// </summary>
        /// <param name="target">The target object to be converted to JSON.</param>
        /// <returns>A JSON representation of the <paramref name="target"/> object.</returns>
        public string Serialize(object target)
        {
            var writer = new StringWriter();
            Serialize(target, writer);
            return writer.ToString();
        }

        /// <summary>
        /// Serializes the specified object to its equivalent JSON representation.
        /// </summary>
        /// <param name="target">The target object to be converted to JSON.</param>
        /// <param name="writer">The writer to which a JSON representation of the <paramref name="target"/> object
        /// is written.</param>
        public void Serialize(object target, TextWriter writer) => Serialize(target, writer.Write, writer.Write, 0);

        /// <summary>
        /// Serializes the specified object to its equivalent JSON representation.
        /// </summary>
        /// <param name="target">The target object to be converted to JSON.</param>
        /// <param name="builder">The builder to which a JSON representation of the <paramref name="target"/> object
        /// is written.</param>
        public void Serialize(object target, StringBuilder builder)
            => Serialize(target, x => builder.Append(x), x => builder.Append(x), 0);

        private string GetIndent(int indentLevel)
        {
            // For multi-threading reasons, take a single reference to m_Indents here.
            var indents = _indents;

            // If the indent is cached, return it. This is the quick happy path.
            var length = indents.Length;
            if (indentLevel < length)
            {
                return indents[indentLevel];
            }

            if (indentLevel > 50)
            {
                throw new InvalidOperationException(
                    "The nesting level is too great or the input may contain cyclic references.");
            }

            // Make a new cache and store it. For threading reasons, use a local variable and only assign to m_Indents once.
            var newIndents = new string[indentLevel + 1];
            indents.CopyTo(newIndents, 0);
            for (var i = length; i <= indentLevel; i++)
            {
                newIndents[i] = _indented
                    ? Environment.NewLine + new string(' ', i * 2)
                    : "";
            }
            _indents = newIndents;

            // And finally return the required indent.
            return newIndents[indentLevel];
        }

        private void Serialize(object target, Action<string> writeString, Action<char> writeChar, int indentLevel)
        {
            if (target == null)
            {
                writeString("null");
            }
            else
            {
                var type = target.GetType();
                Action<object, Action<string>, Action<Char>> handler;
                if (_basicHandlers.TryGetValue(type, out handler))
                {
                    handler(target, writeString, writeChar);
                }
                else if (target is Enum)
                {
                    SerializeEnum(type, target, writeString, writeChar);
                }
                else if (target is IDictionary)
                {
                    SerializeDictionary(target, writeString, writeChar, indentLevel);
                }
                else if (target is IEnumerable)
                {
                    SerializeEnumerable(target, writeString, writeChar, indentLevel);
                }
                else
                {
                    SerializeObject(target, writeString, writeChar, indentLevel);
                }
            }
        }

        private void SerializeDictionary(object target, Action<string> writeString, Action<char> writeChar, int indentLevel)
        {
            var dictionary = (IDictionary) target;

            if (dictionary.Count == 0)
            {
                writeString("{}");
            }
            else
            {
                writeChar('{');

                var newIndentLevel = indentLevel + 1;
                var newIndent = GetIndent(newIndentLevel);
                bool isFirstElement = true;
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (isFirstElement)
                    {
                        isFirstElement = false;
                    }
                    else
                    {
                        writeChar(',');
                    }
                    writeString(newIndent);
                    SerializeString(entry.Key.ToString(), writeString, writeChar);
                    writeString(_keyValueSeparator);
                    Serialize(entry.Value, writeString, writeChar, newIndentLevel);
                }

                writeString(GetIndent(indentLevel));
                writeChar('}');
            }
        }

        private void SerializeObject(object target, Action<string> writeString, Action<char> writeChar, int indentLevel)
        {
            var properties = target.GetType()
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(member => member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property)
                .Select(member => new
                {
                    name = member.Name,
                    value = member.MemberType == MemberTypes.Field
                        ? ((FieldInfo) member).GetValue(target)
                        : ((PropertyInfo) member).GetValue(target, new object[0])
                })
                .ToList();

            if (properties.Count == 0)
            {
                writeString("{}");
            }
            else
            {
                writeChar('{');

                var newIndentLevel = indentLevel + 1;
                var newIndent = GetIndent(newIndentLevel);
                bool isFirstElement = true;
                foreach (var property in properties)
                {
                    if (isFirstElement)
                    {
                        isFirstElement = false;
                    }
                    else
                    {
                        writeChar(',');
                    }
                    writeString(newIndent);
                    SerializeString(ToCamelCase(property.name), writeString, writeChar);
                    writeString(_keyValueSeparator);
                    Serialize(property.value, writeString, writeChar, newIndentLevel);
                }

                writeString(GetIndent(indentLevel));
                writeChar('}');
            }
        }

        private static string ToCamelCase(string value)
        {
            if (value.Length == 0 || !char.IsUpper(value[0]))
            {
                return value;
            }

            char[] chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; ++i)
            {
                if (!(i > 0 && i + 1 < chars.Length) || char.IsUpper(chars[i + 1]))
                {
                    chars[i] = char.ToLower(chars[i], CultureInfo.InvariantCulture);
                }
                else
                {
                    break;
                }
            }
            return new string(chars);
        }

        private void SerializeEnumerable(object target, Action<string> writeString, Action<char> writeChar, int indentLevel)
        {
            var enumerable = (IEnumerable) target;

            writeChar('[');

            var newIndentLevel = indentLevel + 1;
            var newIndent = GetIndent(newIndentLevel);
            bool isFirstElement = true;
            bool isEmpty = true;
            foreach (var item in enumerable)
            {
                isEmpty = false;
                if (isFirstElement)
                {
                    isFirstElement = false;
                }
                else
                {
                    writeChar(',');
                }
                writeString(newIndent);
                Serialize(item, writeString,writeChar, newIndentLevel);
            }

            if (!isEmpty)
            {
                writeString(GetIndent(indentLevel));
            }
            writeChar(']');
        }

        private void SerializeEnum(Type enumType, object target, Action<string> writeString, Action<char> writeChar)
        {
            var validNames = new HashSet<string>(Enum.GetNames(enumType));
            var stringValue = Enum.Format(enumType, target, "F");
            if (stringValue.Split(new[] {", "}, StringSplitOptions.None).All(validNames.Contains))
            {
                SerializeString(stringValue, writeString, writeChar);
            }
            else
            {
                SerializeUnrecognisedEnumValue(target, writeString, writeChar);
            }
        }

        private void SerializeUnrecognisedEnumValue(object target, Action<string> writeString, Action<char> writeChar)
        {
            var value = (Enum) target;
            switch (value.GetTypeCode())
            {
                case TypeCode.Byte:
                    _basicHandlers[typeof(byte)](target, writeString, writeChar);
                    break;

                case TypeCode.SByte:
                    _basicHandlers[typeof(sbyte)](target, writeString, writeChar);
                    break;

                case TypeCode.Int16:
                    _basicHandlers[typeof(short)](target, writeString, writeChar);
                    break;

                case TypeCode.UInt16:
                    _basicHandlers[typeof(ushort)](target, writeString, writeChar);
                    break;

                case TypeCode.Int32:
                    _basicHandlers[typeof(int)](target, writeString, writeChar);
                    break;

                case TypeCode.UInt32:
                    _basicHandlers[typeof(uint)](target, writeString, writeChar);
                    break;

                case TypeCode.Int64:
                    _basicHandlers[typeof(long)](target, writeString, writeChar);
                    break;

                case TypeCode.UInt64:
                    _basicHandlers[typeof(ulong)](target, writeString, writeChar);
                    break;

                default:
                    // Shouldn't hit this case, as all of the valid Enum base-types are covered above.
                    writeString("null");
                    break;
            }
        }

        private void SerializeDateTimeOffset(object target, Action<string> writeString, Action<char> writeChar)
            => SerializeDateTime(((DateTimeOffset) target).DateTime, writeString, writeChar);

        private void SerializeDateTime(object target, Action<string> writeString, Action<char> writeChar)
            => SerializeDateTime((DateTime) target, writeString, writeChar);

        private void SerializeDateTime(DateTime dateTime, Action<string> writeString, Action<char> writeChar)
        {
            if (dateTime.Kind != DateTimeKind.Utc)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            writeChar('"');
            writeString(dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ")); // Standard ISO 8601 UTC date format.
            writeChar('"');
        }

        private void SerializeBool(object target, Action<string> writeString, Action<char> writeChar)
            => writeString(((bool) target) ? "true" : "false");

        private void SerializeWithToString(object target, Action<string> writeString, Action<char> writeChar)
            => writeString(target.ToString());

        private void SerializeFloat(object target, Action<string> writeString, Action<char> writeChar)
        {
            var value = (float) target;
            if (float.IsInfinity(value))
            {
                writeString(float.IsPositiveInfinity(value) ? "\"Infinity\"" : "\"-Infinity\"");
            }
            else if (float.IsNaN(value))
            {
                writeString("\"NaN\"");
            }
            else
            {
                var stringValue = value.ToString("G10", CultureInfo.InvariantCulture);
                writeString(stringValue);
                if (stringValue.IndexOfAny(FloatingPointIndicatorChars) == -1)
                {
                    writeString(".0");
                }
            }
        }

        private void SerializeDouble(object target, Action<string> writeString, Action<char> writeChar)
        {
            var value = (double) target;
            if (double.IsInfinity(value))
            {
                writeString(double.IsPositiveInfinity(value) ? "\"Infinity\"" : "\"-Infinity\"");
            }
            else if (double.IsNaN(value))
            {
                writeString("\"NaN\"");
            }
            else
            {
                var stringValue = value.ToString("G17", CultureInfo.InvariantCulture);
                writeString(stringValue);
                if (stringValue.IndexOfAny(FloatingPointIndicatorChars) == -1)
                {
                    writeString(".0");
                }
            }
        }

        /// <summary>
        /// Characters that indicate that a string representation of a number represents a floating point number, rather than just an integer.
        /// </summary>
        private static readonly char[] FloatingPointIndicatorChars = new char[] {'.', 'E', 'e'};

        private void SerializeDecimal(object target, Action<string> writeString, Action<char> writeChar)
        {
            var value = (Decimal) target;
            var stringValue = value.ToString(null, CultureInfo.InvariantCulture);
            writeString(stringValue);
            if (stringValue.IndexOf('.') == -1)
            {
                writeString(".0");
            }
        }

        private void SerializeChar(object target, Action<string> writeString, Action<char> writeChar)
        {
            writeChar('"');
            SerializeSingleChar((char) target, writeString, writeChar);
            writeChar('"');
        }

        private void SerializeGuid(object target, Action<string> writeString, Action<char> writeChar)
        {
            writeChar('"');
            writeString(target.ToString());
            writeChar('"');
        }

        private void SerializeString(object target, Action<string> writeString, Action<char> writeChar)
        {
            writeChar('"');

            var str = (string) target;
            var length = str.Length;
            for (int i = 0; i < length; i++)
            {
                SerializeSingleChar(str[i], writeString, writeChar);
            }

            writeChar('"');
        }

        private void SerializeSingleChar(char ch, Action<string> writeString, Action<char> writeChar)
        {
            if ((ch & 0xff80) == 0) // Is an ascii char, in the range 0 - 127?
            {
                var escape = AsciiCharEscapes[ch];
                if (escape != null)
                {
                    writeString(escape);
                }
                else
                {
                    writeChar(ch);
                }
            }
            else
            {
                writeString("\\u");
                writeString(((int) ch).ToString("x4"));
            }
        }

        private static readonly string[] AsciiCharEscapes;

        static JsonSerializer()
        {
            AsciiCharEscapes = new string[128];
            for (var i = 0; i < 32; i++)
            {
                AsciiCharEscapes[i] = "\\u" + i.ToString("x4");
            }
            AsciiCharEscapes[127] = "\\u" + 127.ToString("x4");
            AsciiCharEscapes['\"'] = "\\\"";
            AsciiCharEscapes['\\'] = "\\\\";
            AsciiCharEscapes['\b'] = "\\b";
            AsciiCharEscapes['\f'] = "\\f";
            AsciiCharEscapes['\n'] = "\\n";
            AsciiCharEscapes['\r'] = "\\r";
            AsciiCharEscapes['\t'] = "\\t";
        }
    }
}
