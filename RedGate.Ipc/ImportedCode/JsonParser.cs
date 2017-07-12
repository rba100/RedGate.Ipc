using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RedGate.Ipc.ImportedCode
{
    internal sealed class JsonParser
    {
        private static char[] _null = { 'n', 'u', 'l', 'l' };
        private static char[] _true = { 't', 'r', 'u', 'e' };
        private static char[] _false = { 'f', 'a', 'l', 's', 'e' };

        public JsonValue Parse(string json)
        {
            var reader = new StringCharReader(json);
            return ParseJsonValue(reader);
        }

        public JsonValue Parse(StreamReader jsonTextStream)
        {
            var reader = new StreamCharReader(jsonTextStream);
            return ParseJsonValue(reader);
        }

        private JsonValue ParseJsonValue(ICharReader charReader)
        {
            var leadingCharacter = charReader.TrimThenPeek();
            if (!leadingCharacter.HasValue) throw new JsonException("Unexpected end of stream");
            var valueType = IdentifyValueType(leadingCharacter.Value);

            switch (valueType)
            {
                case JsonValueType.String:
                    return ParseString(charReader);
                case JsonValueType.Number:
                    return ParseNumber(charReader);
                case JsonValueType.Object:
                    return ParseObject(charReader);
                case JsonValueType.Array:
                    return ParseArray(charReader);
                case JsonValueType.True:
                    return ParseTrue(charReader);
                case JsonValueType.False:
                    return ParseFalse(charReader);
                case JsonValueType.Null:
                    return ParseNull(charReader);
                case JsonValueType.Unrecognised:
                    throw new JsonException($"Unexpected character '{leadingCharacter.Value}'");
                default:
                    throw new ArgumentOutOfRangeException(nameof(valueType), valueType, null);
            }
        }

        private JsonValue ParseObject(ICharReader charReader)
        {
            var members = new List<JsonObjectMember>();

            if (!charReader.TrimRead('{'))
            {
                throw new JsonException("Expected '{' when parsing json object.");
            }

            var peek = charReader.TrimThenPeek();
            if (!peek.HasValue) throw new JsonException("Unterminated object");

            while (peek.Value != '}')
            {
                if (members.Any())
                {
                    if (!charReader.TrimRead(','))
                    {
                        throw new JsonException("Missing comma separater between array items.");
                    }
                }
                var member = ParseObjectMember(charReader);
                members.Add(member);
                peek = charReader.TrimThenPeek();
                if (!peek.HasValue) throw new JsonException("Unterminated object");
            }
            charReader.Read(); // }
            return new JsonObject(members);
        }

        private JsonObjectMember ParseObjectMember(ICharReader charReader)
        {
            var name = ParseString(charReader);
            if (!charReader.TrimRead(':'))
            {
                throw new JsonException("expected ':' after member name definition");
            }
            var value = ParseJsonValue(charReader);
            return new JsonObjectMember(name.Value, value);
        }

        private static readonly char[] Numerics = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', 'e', 'E', '-' };

        private JsonValue ParseNumber(ICharReader charReader)
        {
            var sb = new StringBuilder();
            var peek = charReader.Peek();

            while (peek.HasValue && Numerics.Contains(peek.Value))
            {
                sb.Append(charReader.Read());
                peek = charReader.Peek();
            }

            return new JsonNumber(sb.ToString());
        }

        private JsonValue ParseArray(ICharReader charReader)
        {
            charReader.TrimRead(); // [

            var items = new List<JsonValue>();
            while (charReader.TrimThenPeek() != ']')
            {
                if (items.Any())
                {
                    if (!charReader.TrimRead(','))
                    {
                        throw new JsonException("Missing comma separater between array items.");
                    }
                }
                items.Add(ParseJsonValue(charReader));
            }
            charReader.Read(); // ]

            return new JsonArray(items);
        }

        private JsonString ParseString(ICharReader charReader)
        {
            var delimiter = charReader.TrimRead();
            if (delimiter != '\'' && delimiter != '"') throw new JsonException("Strings must be delimiated with either single or double quotes");

            var sb = new StringBuilder();
            var c = GetNextStringCharacter(charReader, delimiter);
            while (c.HasValue)
            {
                sb.Append(c);
                c = GetNextStringCharacter(charReader, delimiter);
            }

            return new JsonString(sb.ToString());
        }

        private char? GetNextStringCharacter(ICharReader charReader, char? delimiter)
        {
            var c = charReader.Read();
            if (!c.HasValue) throw new JsonException("Unterminated string");
            if (c == delimiter) return null;
            if (CharRequiresEscapeInString(c.Value)) throw new JsonException($"Unescaped '{c}' in string");
            if (c != '\\') return c;

            c = charReader.Read();
            if (!c.HasValue) throw new JsonException("Unterminated string");

            char fromShortCode;
            if (ShortEscapeDecodables.TryGetValue(c.Value, out fromShortCode))
            {
                return fromShortCode;
            }

            if (c == 'u') return GetUnicodeSymbol(charReader);

            throw new JsonException($"Unrecognised escape sequence '\\{c}'");
        }

        private char? GetUnicodeSymbol(ICharReader charReader)
        {
            var sb = new StringBuilder(8);
            for (var i = 0; i < 4; i++)
            {
                var c = charReader.Read();
                if (!c.HasValue) throw new JsonException("Unterminated string");
                if (char.IsDigit(c.Value) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                {
                    sb.Append(c.Value);
                }
                else
                {
                    throw new JsonException($"Invalid character '{c.Value}' in hexidecimal unicode notation");
                }
            }

            var hexString = sb.ToString();
            var sChar = (ushort)(Convert.ToUInt16(hexString.Substring(0, 2), 16) << 8);
            sChar += Convert.ToUInt16(hexString.Substring(2, 2), 16);
            return Convert.ToChar(sChar);
        }

        private static readonly Dictionary<char, char> ShortEscapeDecodables
            = new Dictionary<char, char>
            {
                {'\"', '"'},
                {'\\', '\\'},
                {'/', '/'},
                {'b', '\b'},
                {'f', '\f'},
                {'n', '\n'},
                {'r', '\r'},
                {'t', '\t'}
            };

        private bool CharRequiresEscapeInString(char c)
        {
            return c <= '\u001f';
        }

        private JsonValue ParseTrue(ICharReader charReader)
        {
            foreach (var c in _true)
            {
                if (c != charReader.Read()) throw new JsonException($"Unexpected character '{c}' whilst parsing 'true'");
            }
            return new JsonTrue();
        }

        private JsonValue ParseFalse(ICharReader charReader)
        {
            foreach (var c in _false)
            {
                if (c != charReader.Read()) throw new JsonException($"Unexpected character '{c}' whilst parsing 'false'");
            }
            return new JsonFalse();
        }

        private JsonValue ParseNull(ICharReader charReader)
        {
            foreach (var c in _null)
            {
                if (c != charReader.Read()) throw new JsonException($"Unexpected character '{c}' whilst parsing 'null'");
            }
            return new JsonNull();
        }

        private static JsonValueType IdentifyValueType(char leadingCharacter)
        {
            switch (leadingCharacter)
            {
                case '\"': return JsonValueType.String;
                case '\'': return JsonValueType.String;
                case '{': return JsonValueType.Object;
                case '[': return JsonValueType.Array;
                case 't': return JsonValueType.True;
                case 'f': return JsonValueType.False;
                case 'n': return JsonValueType.Null;
            }

            if (char.IsDigit(leadingCharacter)
                || leadingCharacter == '-') return JsonValueType.Number;

            return JsonValueType.Unrecognised;
        }
    }

    static class ExtentionMethods
    {
        public static char? TrimThenPeek(this ICharReader charReader)
        {
            AdvanceWhitespace(charReader);
            return charReader.Peek();
        }

        public static char? TrimRead(this ICharReader charReader)
        {
            AdvanceWhitespace(charReader);
            return charReader.Read();
        }

        public static bool TrimRead(this ICharReader charReader, char c)
        {
            AdvanceWhitespace(charReader);
            return charReader.Read(c);
        }

        private static void AdvanceWhitespace(ICharReader charReader)
        {
            var peek = charReader.Peek();
            while (peek.HasValue && char.IsWhiteSpace(peek.Value))
            {
                charReader.Read();
                peek = charReader.Peek();
            }
        }
    }

    internal class JsonException : Exception
    {
        public JsonException(string message) : base(message)
        {
        }
    }

    internal class JsonObject : JsonValue
    {
        public IEnumerable<JsonObjectMember> Members { get; }

        public JsonObject(IEnumerable<JsonObjectMember> members)
        {
            Members = members;
        }

        public JsonValue this[string index]
        {
            get { return Members.FirstOrDefault(m => m.Name == index)?.Value; }
        }
    }

    internal class JsonObjectMember
    {
        public string Name { get; }
        public JsonValue Value { get; }

        public JsonObjectMember(string name, JsonValue value)
        {
            Name = name;
            Value = value;
        }
    }

    internal class JsonTrue : JsonValue
    {

    }

    internal class JsonFalse : JsonValue
    {

    }

    internal class JsonNull : JsonValue
    {

    }

    internal class JsonString : JsonValue
    {
        public string Value { get; }

        public JsonString(string value)
        {
            Value = value;
        }
    }

    internal class JsonNumber : JsonValue
    {
        public string StringRepresentation { get; }

        public JsonNumber(string stringRepresentation)
        {
            StringRepresentation = stringRepresentation;
        }
    }

    internal class JsonArray : JsonValue
    {
        public IList<JsonValue> Items { get; }

        public JsonArray(IList<JsonValue> items)
        {
            Items = items;
        }
    }

    enum JsonValueType { String, Number, Object, Array, True, False, Null, Unrecognised }

    internal abstract class JsonValue
    {
    }

    internal interface ICharReader
    {
        /// <summary>
        /// Peek() returns the next character in the stream without
        /// advancing the position counter. Returns null if there
        /// are no further characters in the stream.
        /// </summary>
        char? Peek();

        /// <summary>
        /// Returns the next character in the stream, or null if EoS.
        /// </summary>
        char? Read();

        /// <summary>
        /// If the supplied character is the next character in the
        /// stream the position counter is advanced and the method
        /// returns true. Otherwise the counter is not advanced and
        /// the method returns false.
        /// </summary>
        bool Read(char c);
    }

    internal class StreamCharReader : ICharReader
    {
        private readonly StreamReader _stream;

        public StreamCharReader(StreamReader stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            _stream = stream;
        }

        public char? Peek()
        {
            var c = _stream.Peek();
            return c == -1
                ? null
                : (char?)c;
        }

        public char? Read()
        {
            var c = _stream.Read();
            return c == -1
                ? null
                : (char?)c;
        }

        public bool Read(char c)
        {
            if (c != Peek()) return false;
            _stream.Read();
            return true;
        }
    }


    internal class StringCharReader : ICharReader
    {
        private readonly string _string;
        private int _position;

        public StringCharReader(string s)
        {
            _string = s;
        }

        public char? Peek()
        {
            return Read(_position);
        }

        public char? Read()
        {
            return Read(_position++);
        }

        public bool Read(char c)
        {
            if (Read(_position) != c) return false;
            _position++;
            return true;
        }

        private char? Read(int position)
        {
            if (position > _string.Length - 1) return null;
            return _string[position];
        }
    }
}
