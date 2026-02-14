using System;
using System.Globalization;
using System.IO;
using System.Text;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Stl
{
    public class StlStreamReader : StreamReader
    {
        public const char SpaceChar = ' ';

        public const char TabChar = '\t';

        public const char CRChar = '\r';

        public const char LFChar = '\n';

        private int _endOfLinePointer;

        private const int MaxChars = 1024;
        private char[] _charStream = new char[MaxChars];
        private string _charString = new string('\0', MaxChars);

        private int _charPosition;

        public int Line { get; private set; }

        public int Column
        {
            get => (int)(BaseStream.Position - _endOfLinePointer);
        }

        public float ReadTokenAsFloat(bool required = true)
        {
            var token = ReadToken(required);
            if (token == 0)
            {
                return -1f;
            }
            if (!GetTokenAsFloat(out var value))
            {
                throw new Exception($"Expected float value at row {Line} and col {Column}");
            }
            return value;
        }

        public string ReadTokenAsString(bool required = true, bool ignoreSpaces = false)
        {
            var result = ReadToken(required, ignoreSpaces);
            return result == 0 ? null : GetTokenAsString();
        }

        public bool TokenStartsWith(string value)
        {
            if (value.Length > _charStream.Length)
            {
                return false;
            }
            for (var i = 0; i < value.Length; i++)
            {
                if (_charStream[i] != value[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetCharAt(int index)
        {
            return _charStream[index];
        }

        public string GetTokenAsString()
        {
            return new string(_charStream, 0, _charPosition);
        }

        private void UpdateStringFromCharString()
        {
            _charString = new string(_charStream, 0, _charPosition);
        }

        public bool GetTokenAsFloat(out float value)
        {
            UpdateStringFromCharString();
            return float.TryParse(_charString, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        public bool GetTokenAsInt(out int value)
        {
            UpdateStringFromCharString();
            return int.TryParse(_charString, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        public long ReadToken(bool required = true, bool ignoreSpaces = false)
        {
            _charPosition = 0;
            var hasToContinue = true;
            var hasEndOfLine = false;
            while (hasToContinue)
            {
                var peek = Peek();
                if (peek >= 0)
                {
                    var character = (char)peek;
                    switch (character)
                    {
                        case LFChar:
                            {
                                Read();
                                hasEndOfLine = true;
                                hasToContinue = false;
                                break;
                            }
                        case SpaceChar:
                        case TabChar:
                        case CRChar:
                            {
                                Read();
                                break;
                            }
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
            hasToContinue = !hasEndOfLine && !EndOfStream;
            while (hasToContinue)
            {
                var peek = Peek();
                if (peek >= 0)
                {
                    var character = (char)peek;

                    switch (character)
                    {
                        case LFChar:
                            {
                                hasToContinue = false;
                                break;
                            }
                        case CRChar:
                            {
                                Read();
                                break;
                            }
                        case SpaceChar:
                        case TabChar:
                            {
                                if (ignoreSpaces)
                                {
                                    _charStream[_charPosition++] = (character);
                                    Read();
                                }
                                else
                                {
                                    hasToContinue = false;
                                }
                                break;
                            }
                        default:
                            {
                                _charStream[_charPosition++] = (character);
                                Read();
                                break;
                            }
                    }
                }
                else
                {
                    hasToContinue = false;
                }
            }
            if (required && _charPosition == 0)
            {
                throw new Exception($"Required token missing at row {Line} and col {Column}");
            }
            return _charPosition == 0 ? 0 : HashUtils.GetHash(_charStream, Mathf.Min(_charPosition, MaxChars));
        }

        public StlStreamReader(AssetLoaderContext assetLoaderContext, Stream stream) : base(stream: stream, encoding: Encoding.UTF8, bufferSize: 1024, detectEncodingFromByteOrderMarks: true, leaveOpen: !assetLoaderContext.Options.CloseStreamAutomatically)
        {
        }
    }
}
