using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Parses a JSON document from a binary stream. This implementation utilizes a <see cref="BinaryReader"/>
    /// and offers functionality for tokenizing and constructing JSON objects, arrays, and values.
    /// </summary>
    public class JsonParser
    {
        // Token checks for "false", "null", and "true" in hash form.
        private const long _false_token = 7096547112153268318;
        private const long _null_token = 6774539739450526444;
        private const long _true_token = 6774539739450702579;

        // Constants for various characters used during parsing.
        private const char AddChar = '+';
        private const char BackslashChar = '\\';
        private const char CloseCurlyChar = '}';
        private const char CloseSquareChar = ']';
        private const char ColonChar = ':';
        private const char CommaChar = ',';
        private const char CRChar = '\r';
        private const char DotChar = '.';
        private const char LFChar = '\n';
        private const char OpenCurlyChar = '{';
        private const char OpenSquareChar = '[';
        private const char QuoteChar = '"';
        private const char SpaceChar = ' ';
        private const char SubChar = '-';
        private const char TabChar = '\t';

        // The underlying reader from which JSON data is read.
        private readonly BinaryReader _binaryReader;

        // Determines whether string keys are created and stored during parsing.
        private readonly bool _createKeys;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonParser"/> class with the specified <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="binaryReader">The <see cref="BinaryReader"/> to read JSON data from.</param>
        /// <param name="createKeys">
        /// A value indicating whether parsed string keys should be stored within 
        /// the resulting <see cref="JsonValue"/> objects for later retrieval.
        /// </param>
        public JsonParser(BinaryReader binaryReader, bool createKeys = false)
        {
            _binaryReader = binaryReader;
            _createKeys = createKeys;
        }

        /// <summary>
        /// Specifies the recognized JSON value types.
        /// </summary>
        public enum JsonValueType
        {
            /// <summary>
            /// Represents a JSON object type ("{...}").
            /// </summary>
            Object,
            /// <summary>
            /// Represents a JSON array type ("[...]").
            /// </summary>
            Array,
            /// <summary>
            /// Represents a numeric JSON value.
            /// </summary>
            Number,
            /// <summary>
            /// Represents a string JSON value.
            /// </summary>
            String,
            /// <summary>
            /// Represents the literal 'true'.
            /// </summary>
            True,
            /// <summary>
            /// Represents the literal 'false'.
            /// </summary>
            False,
            /// <summary>
            /// Represents the literal 'null'.
            /// </summary>
            Null,
            /// <summary>
            /// Represents an unknown or unrecognized type.
            /// </summary>
            Unknown
        }

        /// <summary>
        /// Parses the root JSON value, which must be an object (i.e., enclosed in curly braces).
        /// </summary>
        /// <returns>A <see cref="JsonValue"/> representing the root object.</returns>
        /// <exception cref="Exception">Thrown when the root token is not a '{' character.</exception>
        public JsonValue ParseRootValue()
        {
            var tokenPosition = ReadToken(out var tokenLength, out var tokenHash, out var tokenString, out var tokenChar);
            if (tokenChar != OpenCurlyChar)
            {
                throw new Exception("Expecting: object");
            }

            var rootValue = new JsonValue(this, (int)tokenPosition, (int)tokenLength, JsonValueType.Object);
            ParseKeysAndValues(ref rootValue);
            return rootValue;
        }

        /// <summary>
        /// Parses JSON values, either within an array context ("[ ]") or as part of an object's key-value pairs.
        /// This method reads tokens from the underlying stream and constructs <see cref="JsonValue"/> children for 
        /// the given <paramref name="parentValue"/>.
        /// </summary>
        /// <param name="parentValue">The parent <see cref="JsonValue"/> to which parsed child values are added.</param>
        /// <param name="insideArray">
        /// <see langword="true"/> if the values belong to an array (e.g. "[...]"); 
        /// <see langword="false"/> if they are part of an object's properties.
        /// </param>
        /// <exception cref="Exception">Thrown if an unexpected token is encountered.</exception>
        public void ParseValues(ref JsonValue parentValue, bool insideArray)
        {
            parseValue:
            var tokenPosition = ReadToken(out var tokenLength, out var tokenHash, out var tokenIsString, out var tokenChar);
            var key = parentValue.JsonParser._createKeys
                ? JsonValue.GetStringValue(parentValue.JsonParser, tokenPosition, (int)tokenLength)
                : null;

            switch (tokenChar)
            {
                case OpenCurlyChar:
                    // Create a new object value and parse its key-value pairs.
                    {
                        var value = new JsonValue(parentValue.JsonParser, (int)tokenPosition, (int)tokenLength, JsonValueType.Object);
                        ParseKeysAndValues(ref value);
                        parentValue.AddChild(0, value, key);
                        break;
                    }
                case OpenSquareChar:
                    // Create a new array value and parse its array elements.
                    {
                        var value = new JsonValue(parentValue.JsonParser, (int)tokenPosition, (int)tokenLength, JsonValueType.Array);
                        ParseValues(ref value, true);
                        parentValue.AddChild(0, value, key);
                        break;
                    }
                default:
                    {
                        // If the token was recognized as a string:
                        if (tokenIsString)
                        {
                            var value = new JsonValue(parentValue.JsonParser, (int)tokenPosition, (int)tokenLength, JsonValueType.String);
                            parentValue.AddChild(0, value, key);
                            break;
                        }

                        // If the token is numeric (digits, decimal point, plus/minus sign):
                        if (char.IsDigit(tokenChar) || tokenChar == DotChar || tokenChar == SubChar || tokenChar == AddChar)
                        {
                            var value = new JsonValue(parentValue.JsonParser, (int)tokenPosition, (int)tokenLength, JsonValueType.Number);
                            parentValue.AddChild(0, value, key);
                        }
                        else
                        {
                            // Check if the token is "true", "false", or "null" by comparing hashes.
                            JsonValueType valueType;
                            switch (tokenHash)
                            {
                                case _true_token:
                                    valueType = JsonValueType.True;
                                    break;
                                case _false_token:
                                    valueType = JsonValueType.False;
                                    break;
                                case _null_token:
                                    valueType = JsonValueType.Null;
                                    break;
                                default:
                                    // If parsing an array, a closing bracket ends the array.
                                    if (insideArray && tokenChar == CloseSquareChar)
                                    {
                                        return;
                                    }
                                    valueType = JsonValueType.Unknown;
                                    break;
                            }
                            var value = new JsonValue(parentValue.JsonParser, (int)tokenPosition, (int)tokenLength, valueType);
                            parentValue.AddChild(0, value, key);
                        }
                        break;
                    }
            }

            // If we're parsing an array, look for ',' or ']' next.
            if (insideArray)
            {
                tokenPosition = ReadToken(out tokenLength, out tokenHash, out tokenIsString, out tokenChar);
                if (tokenChar == CommaChar)
                {
                    goto parseValue;
                }
                if (tokenChar != CloseSquareChar)
                {
                    throw new Exception("Expecting: square close");
                }
            }
        }

        /// <summary>
        /// Reads a JSON token from the underlying <see cref="BinaryReader"/> and returns its position, length, hash, 
        /// and additional characteristics (e.g., whether it is a string).
        /// </summary>
        /// <param name="tokenLength">Outputs the length of the token in bytes.</param>
        /// <param name="tokenHash">Outputs the computed hash value for the token's contents.</param>
        /// <param name="isString">Outputs a value indicating whether this token is delimited as a string.</param>
        /// <param name="initialChar">Outputs the first character of the token (for single-character tokens) or the delimiter (for strings).</param>
        /// <returns>The position in the stream at which the token begins.</returns>
        public long ReadToken(out long tokenLength, out long tokenHash, out bool isString, out char initialChar)
        {
            var tokenPosition = 0L;
            tokenLength = 0L;
            tokenHash = 0L;
            isString = false;
            initialChar = default;
            var hasToContinue = true;
            int peek;

            // Skip whitespace and control characters.
            while (hasToContinue)
            {
                peek = _binaryReader.PeekChar();
                if (peek >= 0)
                {
                    var character = (char)peek;
                    switch (character)
                    {
                        case SpaceChar:
                        case TabChar:
                        case CRChar:
                        case LFChar:
                            _binaryReader.Read();
                            break;
                        default:
                            hasToContinue = false;
                            break;
                    }
                }
                else
                {
                    hasToContinue = false;
                }
            }

            // Analyze the next character to determine which token type we're dealing with.
            peek = _binaryReader.PeekChar();
            if (peek >= 0)
            {
                var character = (char)peek;
                switch (character)
                {
                    // Single-character tokens: { } [ ] : ,
                    case OpenCurlyChar:
                    case CloseCurlyChar:
                    case OpenSquareChar:
                    case CloseSquareChar:
                    case ColonChar:
                    case CommaChar:
                        {
                            tokenPosition = _binaryReader.BaseStream.Position;
                            initialChar = (char)_binaryReader.Read();
                            tokenLength = _binaryReader.BaseStream.Position - tokenPosition;
                            break;
                        }
                    // Strings: "..."
                    case QuoteChar:
                        {
                            isString = true;
                            initialChar = (char)peek;
                            tokenHash = HashUtils.GetHashInitialValue();
                            tokenPosition = _binaryReader.BaseStream.Position + 1;
                            var lastPeek = peek;

                            // Read until the closing quote (not preceded by a backslash).
                            for (; ; )
                            {
                                _binaryReader.Read();
                                peek = _binaryReader.PeekChar();
                                if (peek == QuoteChar && lastPeek != BackslashChar)
                                {
                                    break;
                                }
                                tokenHash = HashUtils.GetHash(tokenHash, peek);
                                lastPeek = peek;
                            }
                            // Consume the closing quote.
                            _binaryReader.Read();
                            tokenLength = _binaryReader.BaseStream.Position - tokenPosition - 1;
                            break;
                        }

                    // Numeric literals, booleans, null, or unknown tokens.
                    default:
                        {
                            initialChar = (char)peek;
                            tokenHash = HashUtils.GetHashInitialValue();
                            tokenPosition = _binaryReader.BaseStream.Position;
                            int lastPeek;
                            do
                            {
                                lastPeek = peek;
                                tokenHash = HashUtils.GetHash(tokenHash, peek);
                                _binaryReader.Read();
                                peek = _binaryReader.PeekChar();
                            }
                            while (peek != OpenCurlyChar &&
                                   peek != CloseCurlyChar &&
                                   peek != OpenSquareChar &&
                                   peek != CloseSquareChar &&
                                   peek != ColonChar &&
                                   (peek != CommaChar && lastPeek != BackslashChar) &&
                                   peek != QuoteChar &&
                                   peek != SpaceChar &&
                                   peek != TabChar &&
                                   peek != CRChar &&
                                   peek != LFChar);

                            tokenLength = _binaryReader.BaseStream.Position - tokenPosition;
                            break;
                        }
                }
            }
            return tokenPosition;
        }

        /// <summary>
        /// Parses a series of key-value pairs inside an object, reading keys and their corresponding values.
        /// Key tokens are expected to be strings followed by a colon (<c>:</c>), then a value.
        /// </summary>
        /// <param name="parentValue">
        /// The parent <see cref="JsonValue"/> to which the parsed key-value pairs will be added as children.
        /// </param>
        /// <exception cref="Exception">
        /// Thrown if a key-value pair is missing a colon (<c>:</c>) or if the object does not properly end with <c>}</c>.
        /// </exception>
        private void ParseKeysAndValues(ref JsonValue parentValue)
        {
            parseValue:
            var tokenPosition = ReadToken(out var tokenLength, out var tokenHash, out var tokenIsString, out var tokenChar);

            // If we reach a '}', the object has ended.
            if (tokenChar == CloseCurlyChar)
            {
                return;
            }

            // After the key token, we should see a colon.
            var colonPosition = ReadToken(out var colonLength, out var colonHash, out var colonIsString, out var colonChar);
            if (colonChar != ColonChar)
            {
                throw new Exception("Expecting: colon");
            }

            // Treat the key as a string value (the parser can reconstruct the actual text if needed).
            var value = new JsonValue(parentValue.JsonParser, (int)tokenPosition, (int)tokenLength, JsonValueType.String);
            var key = parentValue.JsonParser._createKeys
                ? JsonValue.GetStringValue(parentValue.JsonParser, tokenPosition, (int)tokenLength)
                : null;

            // Parse the value associated with that key.
            ParseValues(ref value, false);
            parentValue.AddChild(tokenHash, value, key);

            // Next token could be a comma (more pairs) or a closing curly brace.
            tokenPosition = ReadToken(out tokenLength, out tokenHash, out tokenIsString, out tokenChar);
            if (tokenChar == CommaChar)
            {
                goto parseValue;
            }
            if (tokenChar != CloseCurlyChar)
            {
                throw new Exception("Expecting: curly close");
            }
        }

        /// <summary>
        /// Represents a JSON value in the parsed data, including its children (for objects/arrays) 
        /// and its position/length in the underlying stream.
        /// </summary>
        public struct JsonValue : IEnumerable<JsonValue>
        {
            private readonly List<JsonValue> _children;
            private readonly Dictionary<long, int> _hashes;
            private readonly List<string> _keys;

            /// <summary>
            /// Initializes a new instance of the <see cref="JsonValue"/> struct with the given parameters.
            /// </summary>
            /// <param name="jsonParser">
            /// The <see cref="JsonParser"/> used to read and interpret the underlying data.
            /// </param>
            /// <param name="position">The start position of this value in the stream.</param>
            /// <param name="valueLength">The length (in bytes) of this value in the stream.</param>
            /// <param name="type">The <see cref="JsonValueType"/> representing this value.</param>
            public JsonValue(JsonParser jsonParser, int position, int valueLength, JsonValueType type)
            {
                JsonParser = jsonParser;
                Position = position;
                ValueLength = valueLength;
                Type = type;
                _children = new List<JsonValue>();
                _hashes = new Dictionary<long, int>();
                _keys = jsonParser._createKeys ? new List<string>() : null;
            }

            /// <summary>
            /// Gets the total number of direct child values.
            /// </summary>
            public int Count => _children.Count;

            /// <summary>
            /// Gets the <see cref="JsonParser"/> associated with this value.
            /// </summary>
            public JsonParser JsonParser { get; }

            /// <summary>
            /// Gets or sets the position of this JSON value in the underlying stream.
            /// </summary>
            public int Position { get; private set; }

            /// <summary>
            /// Gets the <see cref="JsonValueType"/> for this value.
            /// </summary>
            public JsonValueType Type { get; }

            /// <summary>
            /// Indicates whether this value is valid (i.e., has a non-zero length).
            /// </summary>
            public bool Valid => ValueLength > 0;

            /// <summary>
            /// Gets or sets the length (in bytes) of this value in the underlying stream.
            /// </summary>
            public int ValueLength { get; private set; }

            /// <summary>
            /// Extracts a string value from the underlying stream for the specified 
            /// <paramref name="position"/> and <paramref name="length"/>.
            /// </summary>
            /// <param name="jsonParser">The <see cref="JsonParser"/> that manages the stream.</param>
            /// <param name="position">The position in the stream at which the string value begins.</param>
            /// <param name="length">The length (in bytes) of the string.</param>
            /// <returns>The string read from the stream, or an empty string if an error occurs.</returns>
            public static unsafe string GetStringValue(JsonParser jsonParser, long position, int length)
            {
                var initialPosition = jsonParser._binaryReader.BaseStream.Position;
                jsonParser._binaryReader.BaseStream.Position = position;
                var result = string.Empty;

                if (length > 2048)
                {
                    // For large strings, use heap-allocated arrays
                    var bytes = new byte[length];
                    for (var i = 0; i < length; i++)
                    {
                        bytes[i] = jsonParser._binaryReader.ReadByte();
                    }
                    try
                    {
                        result = Encoding.UTF8.GetString(bytes);
                    }
                    catch
                    {
                        // Ignore encoding errors
                    }
                }
                else
                {
                    // For smaller strings, use stack allocation
                    var bytes = stackalloc byte[length];
                    for (var i = 0; i < length; i++)
                    {
                        bytes[i] = jsonParser._binaryReader.ReadByte();
                    }
                    try
                    {
                        result = Encoding.UTF8.GetString(bytes, length);
                    }
                    catch
                    {
                        // Ignore encoding errors
                    }
                }

                jsonParser._binaryReader.BaseStream.Position = initialPosition;

                result = Regex.Unescape(result);

                return result;
            }

            /// <summary>
            /// Adds a child <see cref="JsonValue"/> to the current value.
            /// </summary>
            /// <param name="hash">A computed hash of the key (if any).</param>
            /// <param name="value">The <see cref="JsonValue"/> to add as a child.</param>
            /// <param name="key">The optional string key associated with this child.</param>
            public void AddChild(long hash, JsonValue value, string key)
            {
                if (_children != null)
                {
                    _children.Add(value);

                    if (_keys != null)
                    {
                        _keys.Add(key);
                    }

                    if (hash != 0 && _hashes != null && !_hashes.ContainsKey(hash))
                    {
                        _hashes.Add(hash, _children.Count - 1);
                    }
                }
            }

            /// <summary>
            /// Adjusts the starting position of this value by the given offset, and reduces
            /// its length accordingly.
            /// </summary>
            /// <param name="offset">The number of bytes to shift from the current position.</param>
            /// <returns>This <see cref="JsonValue"/> after the offset has been applied.</returns>
            public JsonValue AddOffset(int offset)
            {
                Position += offset;
                ValueLength -= offset;
                return this;
            }

            /// <summary>
            /// Copies the bytes of this value into the specified <paramref name="buffer"/>.
            /// </summary>
            /// <param name="buffer">The character array to receive the value's data.</param>
            /// <returns>The number of characters copied into <paramref name="buffer"/>.</returns>
            public int CopyTo(char[] buffer)
            {
                JsonParser._binaryReader.BaseStream.Position = Position;
                return JsonParser._binaryReader.Read(buffer, 0, ValueLength);
            }

            /// <summary>
            /// Returns an enumerator that can iterate through the bytes of this value in the underlying stream.
            /// </summary>
            /// <returns>A <see cref="JsonByteEnumerator"/> for this value.</returns>
            public JsonByteEnumerator GetByteEnumerator()
            {
                return new JsonByteEnumerator(this);
            }

            /// <summary>
            /// Returns an enumerator that can iterate through the characters of this value in the underlying stream.
            /// </summary>
            /// <returns>A <see cref="JsonCharEnumerator"/> for this value.</returns>
            public JsonCharEnumerator GetCharEnumerator()
            {
                return new JsonCharEnumerator(this);
            }

            /// <summary>
            /// Retrieves the child <see cref="JsonValue"/> at the specified <paramref name="index"/>.
            /// </summary>
            /// <param name="index">The zero-based index of the child to retrieve.</param>
            /// <returns>
            /// The child <see cref="JsonValue"/> at <paramref name="index"/>, or 
            /// a default <see cref="JsonValue"/> if the index is out of range.
            /// </returns>
            public JsonValue GetChildAtIndex(int index)
            {
                return _children?[index] ?? default;
            }

            /// <summary>
            /// Retrieves the child <see cref="JsonValue"/> for the given key hash, returning its first child (value).
            /// This is typically used when the child is itself an object or a single value.
            /// </summary>
            /// <param name="hash">The hash of the key for which to retrieve the child.</param>
            /// <returns>
            /// The first child <see cref="JsonValue"/> associated with the key hash; 
            /// otherwise, a default <see cref="JsonValue"/> if not found.
            /// </returns>
            public JsonValue GetChildWithKey(long hash)
            {
                return GetChildWithHash(hash).GetChildAtIndex(0);
            }

            /// <summary>
            /// Returns an enumerator that iterates through this <see cref="JsonValue"/>'s children.
            /// </summary>
            /// <returns>An <see cref="IEnumerator{T}"/> for the child <see cref="JsonValue"/> objects.</returns>
            public IEnumerator<JsonValue> GetEnumerator()
            {
                return new JsonValueEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var hashCode = JsonParser.GetHashCode();
                hashCode = (hashCode * 397) ^ Position;
                hashCode = (hashCode * 397) ^ (int)Type;
                return hashCode;
            }

            /// <summary>
            /// Converts this value to its string representation by reading data from the underlying stream.
            /// </summary>
            /// <returns>A string that represents the contents of this JSON value.</returns>
            public override string ToString()
            {
                return GetStringValue(JsonParser, Position, ValueLength);
            }

            // Retrieves a child using the specified key hash, without advancing the index.
            private JsonValue GetChildWithHash(long hash)
            {
                if (_children != null && _hashes != null && _hashes.TryGetValue(hash, out var index))
                {
                    return _children[index];
                }
                return default;
            }

            // Gets the key string at the specified position (if key creation was enabled).
            private string GetKeyAtIndex(int position)
            {
                if (_keys == null || position < 0 || position >= _keys.Count)
                {
                    return null;
                }
                return _keys[position];
            }

            /// <summary>
            /// Enumerates the bytes of a <see cref="JsonValue"/> from the underlying stream.
            /// </summary>
            public struct JsonByteEnumerator : IEnumerator<byte>, IDisposable
            {
                private readonly long _initialPosition;
                private readonly JsonValue _jsonValue;
                private byte _byte;
                private int _position;

                /// <summary>
                /// Initializes a new instance of the <see cref="JsonByteEnumerator"/> struct for the given <paramref name="jsonValue"/>.
                /// </summary>
                /// <param name="jsonValue">The <see cref="JsonValue"/> to enumerate over.</param>
                public JsonByteEnumerator(JsonParser.JsonValue jsonValue)
                {
                    _jsonValue = jsonValue;
                    _initialPosition = _jsonValue.JsonParser._binaryReader.BaseStream.Position;
                    _jsonValue.JsonParser._binaryReader.BaseStream.Position = _jsonValue.Position;
                    _position = 0;
                    _byte = 0;
                }

                object IEnumerator.Current => Current;

                /// <summary>
                /// Gets the byte at the current position in the enumerator.
                /// </summary>
                public byte Current => _byte;

                /// <inheritdoc/>
                public void Dispose()
                {
                    _jsonValue.JsonParser._binaryReader.BaseStream.Position = _initialPosition;
                }

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    _byte = _jsonValue.JsonParser._binaryReader.ReadByte();
                    return _position++ < _jsonValue.ValueLength;
                }

                /// <inheritdoc/>
                public void Reset()
                {
                    _position = 0;
                    _byte = 0;
                }
            }

            /// <summary>
            /// Enumerates the characters of a <see cref="JsonValue"/> from the underlying stream.
            /// </summary>
            public struct JsonCharEnumerator : IEnumerator<char>, IDisposable
            {
                private readonly long _initialPosition;
                private readonly JsonValue _jsonValue;
                private int _char;

                /// <summary>
                /// Initializes a new instance of the <see cref="JsonCharEnumerator"/> struct for the given <paramref name="jsonValue"/>.
                /// </summary>
                /// <param name="jsonValue">The <see cref="JsonValue"/> to enumerate over.</param>
                public JsonCharEnumerator(JsonParser.JsonValue jsonValue)
                {
                    _jsonValue = jsonValue;
                    _initialPosition = _jsonValue.JsonParser._binaryReader.BaseStream.Position;
                    _jsonValue.JsonParser._binaryReader.BaseStream.Position = _jsonValue.Position;
                    _char = -1;
                }

                object IEnumerator.Current => Current;

                /// <summary>
                /// Gets the character at the current position in the enumerator.
                /// </summary>
                public char Current => (char)_char;

                /// <inheritdoc/>
                public void Dispose()
                {
                    _jsonValue.JsonParser._binaryReader.BaseStream.Position = _initialPosition;
                }

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    _char = _jsonValue.JsonParser._binaryReader.ReadChar();
                    var position = _jsonValue.JsonParser._binaryReader.BaseStream.Position;
                    var read = position - _jsonValue.Position;
                    return _char > -1 && read <= _jsonValue.ValueLength;
                }

                /// <inheritdoc/>
                public void Reset()
                {
                    _char = -1;
                }
            }

            /// <summary>
            /// Enumerates key-value pairs for this <see cref="JsonValue"/> if it was parsed with key creation enabled.
            /// </summary>
            public struct JsonKeyValueEnumerator : IEnumerator<Tuple<string, string>>
            {
                private JsonValue _jsonValue;
                private int _position;

                /// <summary>
                /// Initializes a new instance of the <see cref="JsonKeyValueEnumerator"/> struct for the given <paramref name="jsonValue"/>.
                /// </summary>
                /// <param name="jsonValue">The <see cref="JsonValue"/> whose child key-value pairs will be enumerated.</param>
                public JsonKeyValueEnumerator(JsonParser.JsonValue jsonValue)
                {
                    _jsonValue = jsonValue;
                    _position = -1;
                }

                object IEnumerator.Current => Current;

                /// <summary>
                /// Gets the current key-value pair, represented as a <see cref="Tuple{T1,T2}"/>.
                /// </summary>
                public Tuple<string, string> Current
                {
                    get
                    {
                        var key = _jsonValue.GetKeyAtIndex(_position);
                        var value = _jsonValue.GetChildAtIndex(_position).ToString();
                        return new Tuple<string, string>(key, value);
                    }
                }

                /// <inheritdoc/>
                public void Dispose()
                {
                    // No resources to clean up.
                }

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    return ++_position < _jsonValue.Count;
                }

                /// <inheritdoc/>
                public void Reset()
                {
                    _position = -1;
                }
            }

            /// <summary>
            /// Enumerates the direct child <see cref="JsonValue"/> objects of this instance.
            /// </summary>
            public struct JsonValueEnumerator : IEnumerator<JsonValue>
            {
                private JsonValue _jsonValue;
                private int _position;

                /// <summary>
                /// Initializes a new instance of the <see cref="JsonValueEnumerator"/> struct for the given <paramref name="jsonValue"/>.
                /// </summary>
                /// <param name="jsonValue">The <see cref="JsonValue"/> whose children are to be enumerated.</param>
                public JsonValueEnumerator(JsonParser.JsonValue jsonValue)
                {
                    _jsonValue = jsonValue;
                    _position = -1;
                }

                object IEnumerator.Current => Current;

                /// <summary>
                /// Gets the current <see cref="JsonValue"/> in the enumeration.
                /// </summary>
                public JsonValue Current => _jsonValue.GetChildAtIndex(_position);

                /// <inheritdoc/>
                public void Dispose()
                {
                    // No resources to clean up.
                }

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    return ++_position < _jsonValue.Count;
                }

                /// <inheritdoc/>
                public void Reset()
                {
                    _position = -1;
                }
            }
        }
    }
}
