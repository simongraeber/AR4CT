using System;
using System.Globalization;
using System.IO;
using System.Text;
using TriLibCore.Extensions;
using TriLibCore.Ply.Reader;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Ply
{
    public class PlyStreamReader : StreamReader
    {
        public const char SpaceChar = ' ';

        public const char TabChar = '\t';

        public const char CRChar = '\r';

        public const char LFChar = '\n';

        private const int MaxChars = 1024;
        private const int MaxNumberChars = 24;
        private char[] _charStream;
        private string _charString = new string('\0', MaxNumberChars);
        private int _charPosition;
        public int Line => 0;

        public int Column => 0;

        public long Position { get; private set; }


        private void UpdateStringFromCharString()
        {
            CopyCharStreamToString(_charString, Mathf.Min(_charPosition, MaxNumberChars));
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

        public string ReadTokenAsString(bool required = true, bool ignoreLineWrapChar = false, bool ignoreElementSeparatorChar = true, bool ignoreSpaces = false, bool stopOnHyphen = false)
        {
            var result = ReadToken(required, ignoreSpaces);
            return result == 0 ? null : GetTokenAsString();
        }

        public long ReadToken(bool required = true, bool ignoreSpaces = false)
        {
            _charPosition = 0;
            var hasToContinue = true;
            var hasEndOfLine = false;
            while (hasToContinue && BaseStream.CanRead)
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
                                Position++;
                                hasEndOfLine = true;
                                hasToContinue = false;
                                break;
                            }
                        case SpaceChar:
                        case TabChar:
                        case CRChar:
                            {
                                Read();
                                Position++;
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
                                Position++;
                                break;
                            }
                        case SpaceChar:
                        case TabChar:
                            {
                                if (ignoreSpaces)
                                {
                                    _charStream[_charPosition++] = character;
                                    Read();
                                    Position++;
                                }
                                else
                                {
                                    hasToContinue = false;
                                }
                                break;
                            }
                        default:
                            {
                                _charStream[_charPosition++] = character;
                                Read();
                                Position++;
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

        public string ReadValidTokenAsString()
        {
            long token;
            do
            {
                token = ReadToken(false);
            } while (token == 0 && !EndOfStream);
            UpdateStringFromCharString();
            return _charString;
        }

        public PlyStreamReader(AssetLoaderContext assetLoaderContext, Stream stream) : base(stream: stream, encoding: Encoding.UTF8, bufferSize: 1024, detectEncodingFromByteOrderMarks: true, leaveOpen: !assetLoaderContext.Options.CloseStreamAutomatically)
        {
            _charStream = new char[MaxChars];
        }

        public PlyValue ToSByte()
        {
            ReadValidTokenAsString();
            if (!SByte.TryParse(_charString, out var result))
            {
                return SByte.MaxValue;
            }
            return result;
        }

        public PlyValue ToByte()
        {
            ReadValidTokenAsString();
            if (!Byte.TryParse(_charString, out var result))
            {
                return Byte.MaxValue;
            }
            return result;
        }

        public PlyValue ToInt16()
        {
            ReadValidTokenAsString();
            if (!Int16.TryParse(_charString, out var result))
            {
                return Int16.MaxValue;
            }
            return result;
        }

        public PlyValue ToUInt16()
        {
            ReadValidTokenAsString();
            if (!UInt16.TryParse(_charString, out var result))
            {
                return UInt16.MaxValue;
            }
            return result;
        }


        public int ToInt32NoValue()
        {
            ReadValidTokenAsString();
            return Convert.ToInt32(_charString);
        }

        public PlyValue ToInt32()
        {
            if (!Int32.TryParse(_charString, out var result))
            {
                return Int32.MaxValue;
            }
            return result;
        }

        public PlyValue ToUInt32()
        {
            ReadValidTokenAsString();
            if (!UInt32.TryParse(_charString, out var result))
            {
                return UInt32.MaxValue;
            }
            return result;
        }

        public PlyValue ToSingle()
        {
            ReadValidTokenAsString();
            return Convert.ToSingle(_charString, CultureInfo.InvariantCulture);
        }

        public PlyValue ToDouble()
        {
            ReadValidTokenAsString();
            var value = Convert.ToDouble(_charString, CultureInfo.InvariantCulture);
            value *= PlyReader.PlyConversionPrecision;
            return value;
        }

        protected override void Dispose(bool disposing)
        {
            _charStream.TryToDispose();
            base.Dispose(disposing);
        }
    }
}
