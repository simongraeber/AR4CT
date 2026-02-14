using System;
using System.Globalization;
using System.IO;
using System.Text;
using TriLibCore.Extensions;
using TriLibCore.Obj.Reader;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Obj
{
    public class ObjStreamReader : StreamReader
    {
        public const char NullChar = '\0';

        public const char SpaceChar = ' ';

        public const char TabChar = '\t';

        public const char LineWrapChar = '\\';

        public const char ElementSeparatorChar = '/';

        public const char CommentChar = '#';

        public const char CRChar = '\r';

        public const char LFChar = '\n';

        public const char HyphenChar = '-';

        private const int MaxChars = 1024;
        private const int MaxNumberChars = 24;
        private char[] _charStream;
        private string _charString = new string('\0', MaxNumberChars);
        private int _charPosition;

        public float ReadTokenAsFloat(bool required = true, bool ignoreLineWrapChar = false, bool ignoreElementSeparatorChar = true)
        {
            var token = ReadToken(required, ignoreLineWrapChar, ignoreElementSeparatorChar);
            if (token == 0)
            {
                return -1f;
            }
            if (!GetTokenAsFloat(out var value))
            {
                throw new Exception("Expected float value");
            }
            return value;
        }

        public string ReadTokenAsString(bool required = true, bool ignoreLineWrapChar = false, bool ignoreElementSeparatorChar = true, bool ignoreSpaces = false, bool stopOnHyphen = false)
        {
            var result = ReadToken(required, ignoreLineWrapChar, ignoreElementSeparatorChar, ignoreSpaces, stopOnHyphen);
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

        private unsafe void CopyCharStreamToString(string s, int maxChars)
        {
            maxChars = Mathf.Min(s.Length, maxChars);
            for (var i = 0; i < s.Length; i++)
            {
                fixed (char* c = s)
                {
                    c[i] = '\0';
                }
            }
            fixed (char* c = s)
            {
                for (var i = 0; i < maxChars; i++)
                {
                    c[i] = _charStream[i];
                }
            }
        }

        public string GetTokenAsString()
        {
            var s = new string('\0', _charPosition);
            CopyCharStreamToString(s, _charPosition);
            return s;
        }

        public bool GetTokenAsFloat(out float value)
        {
            UpdateStringFromCharString();
            if (ObjReader.ParseNumbersAsDouble)
            {
                var ret = double.TryParse(_charString, NumberStyles.Any, CultureInfo.InvariantCulture, out var dValue) || _charString.Equals("nan", StringComparison.InvariantCultureIgnoreCase);
                value = (float)(ObjReader.ObjConversionPrecision * dValue);
                return ret;
            }
            else
            {
                return float.TryParse(_charString, NumberStyles.Any, CultureInfo.InvariantCulture, out value) || _charString.Equals("nan", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public bool GetTokenAsInt(out int value)
        {
            UpdateStringFromCharString();
            return int.TryParse(_charString, NumberStyles.Any, CultureInfo.InvariantCulture, out value) || _charString.Equals("nan", StringComparison.InvariantCultureIgnoreCase);
        }
        private void UpdateStringFromCharString()
        {
            CopyCharStreamToString(_charString, Mathf.Min(_charPosition, MaxNumberChars));
        }

        public long ReadToken(bool required = true, bool ignoreLineWrapChar = false, bool ignoreElementSeparatorChar = true, bool ignoreSpaces = false, bool stopOnHyphen = false)
        {
            _charPosition = 0;
            var previousCharacter = '\0';
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
                                previousCharacter = character;
                                Read();
                                hasEndOfLine = true;
                                hasToContinue = false;
                                break;
                            }
                        case CommentChar:
                            {
                                do
                                {
                                    previousCharacter = character;
                                    var read = Read();
                                    if (read < 0)
                                    {
                                        break;
                                    }
                                    character = (char)read;
                                } while (character != LFChar);
                                hasToContinue = false;
                                break;
                            }
                        case SpaceChar:
                        case TabChar:
                        case CRChar:
                        case LineWrapChar:
                        case NullChar:
                            {
                                previousCharacter = character;
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
                        case ElementSeparatorChar:
                            {
                                if (ignoreElementSeparatorChar || stopOnHyphen)
                                {
                                    _charStream[_charPosition++] = character;
                                    previousCharacter = character;
                                    Read();
                                }
                                else
                                {
                                    if (_charPosition == 0)
                                    {
                                        _charStream[_charPosition++] = character;
                                        previousCharacter = character;
                                        Read();
                                    }
                                    hasToContinue = false;
                                }
                                break;
                            }
                        case CRChar:
                        case NullChar:
                            {
                                previousCharacter = character;
                                Read();
                                break;
                            }
                        case HyphenChar when stopOnHyphen && (previousCharacter == SpaceChar || previousCharacter == TabChar):
                            {
                                hasToContinue = false;
                                break;
                            }
                        case SpaceChar:
                        case TabChar:
                            {
                                if (ignoreSpaces || stopOnHyphen)
                                {
                                    _charStream[_charPosition++] = character;
                                    previousCharacter = character;
                                    Read();
                                }
                                else
                                {
                                    hasToContinue = false;
                                }
                                break;
                            }
                        case LineWrapChar:
                            {
                                if (ignoreLineWrapChar || stopOnHyphen)
                                {
                                    _charStream[_charPosition++] = character;
                                }
                                previousCharacter = character;
                                Read();
                                break;
                            }
                        default:
                            {
                                _charStream[_charPosition++] = character;
                                previousCharacter = character;
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
                throw new Exception($"Required token missing");
            }
            return _charPosition == 0 ? 0 : HashUtils.GetHash(_charStream, Mathf.Min(_charPosition, MaxChars));
        }

        public ObjStreamReader(AssetLoaderContext assetLoaderContext, Stream stream) : base(stream: stream, encoding: Encoding.UTF8, bufferSize: 1024, detectEncodingFromByteOrderMarks: true, leaveOpen: !assetLoaderContext.Options.CloseStreamAutomatically)
        {
            _charStream = new char[MaxChars];
        }

        protected override void Dispose(bool disposing)
        {
            _charStream.TryToDispose();
            base.Dispose(disposing);
        }
    }
}
