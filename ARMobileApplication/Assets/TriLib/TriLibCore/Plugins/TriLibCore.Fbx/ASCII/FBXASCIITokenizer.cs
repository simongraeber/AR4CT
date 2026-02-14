using System.IO;
using System.Text;
using TriLibCore.Utils;

namespace TriLibCore.Fbx.ASCII
{
    public class FBXASCIITokenizer : BinaryReader
    {
        public enum TokenType
        {
            None,
            Identifier,
            Character,
            Integer,
            Float,
            String,
            Colon,
            Comma,
            Asterisk,
            LeftBrace,
            RightBrace,
            WS,
            Comment,
            QNAN,
            EOF
        }

        private long _lastPosition;

        public long CharHash;

        private byte[] _pool = new byte[256];

        public FBXASCIITokenizer(Stream stream, bool leaveOpen) : base(stream, Encoding.UTF8, leaveOpen)
        {
        }

        public long Position => BaseStream.Position;

        protected bool EndOfStream => BaseStream.Position >= BaseStream.Length;

        protected void Reset()
        {
            BaseStream.Seek(0, SeekOrigin.Begin);
        }

        private char ReadNextChar()
        {
            if (EndOfStream)
            {
                return '\0';
            }
            _lastPosition = Position;
            return ReadChar();
        }

        public Token PeekToken(int lookahead = 0)
        {
            var position = BaseStream.Position;
            Token token = default;
            for (var i = 0; i <= lookahead; i++)
            {
                token = ReadToken();
            }
            BaseStream.Position = position;
            return token;
        }

        protected Token ReadToken()
        {
            skipSpaces:
            var currentChar = ReadNextChar();
            if (currentChar == '\0')
            {
                return new Token(){Type = TokenType.EOF};
            }
            if (char.IsWhiteSpace(currentChar) || currentChar == '\n' || currentChar == '\r')
            {
                goto skipSpaces;
            }
            if (currentChar == ';')
            {
                while (!EndOfStream && currentChar != '\n')
                {
                    currentChar = ReadNextChar();
                }
                goto skipSpaces;
            }
            var start = BaseStream.Position - 1;
            switch (currentChar)
            {
                case ':':
                    {
                        var token = new Token { Type = TokenType.Colon, Start = start, Length = BaseStream.Position - start };
                        token.HashCode = GetTokenHashCode(token);
                        return token;
                    }
                case ',':
                    {

                        var token = new Token { Type = TokenType.Comma, Start = start, Length = BaseStream.Position - start };
                        token.HashCode = GetTokenHashCode(token);
                        return token;
                    }
                case '*':
                    {
                        var token = new Token { Type = TokenType.Asterisk, Start = start, Length = BaseStream.Position - start };
                        token.HashCode = GetTokenHashCode(token);
                        return token;
                    }
                case '{':
                    {
                        var token = new Token { Type = TokenType.LeftBrace, Start = start, Length = BaseStream.Position - start };
                        token.HashCode = GetTokenHashCode(token);
                        return token;
                    }
                case '}':
                    {
                        var token = new Token { Type = TokenType.RightBrace, Start = start, Length = BaseStream.Position - start };
                        token.HashCode = GetTokenHashCode(token);
                        return token;
                    }
                case '"':
                    {
                        do
                        {
                            currentChar = ReadNextChar();
                        } while (!EndOfStream && currentChar != '"');
                        var token = new Token { Type = TokenType.String, Start = start + 1, Length = (BaseStream.Position - start) - 2 };
                        token.HashCode = GetTokenHashCode(token);
                        return token;
                    }
            }
            start = BaseStream.Position - 1;
            if (MatchFloat(currentChar))
            {
                var token = new Token { Type = TokenType.Float, Start = start, Length = BaseStream.Position - start };
                token.HashCode = GetTokenHashCode(token);
                return token;
            }
            start = BaseStream.Position - 1;
            if (MatchInteger(currentChar))
            {
                var token = new Token { Type = TokenType.Integer, Start = start, Length = BaseStream.Position - start };
                token.HashCode = GetTokenHashCode(token);
                return token;
            }
            start = BaseStream.Position - 1;
            if (MatchIdentifier(currentChar))
            {
                var token = new Token { Type = TokenType.Identifier, Start = start, Length = BaseStream.Position - start };
                token.HashCode = GetTokenHashCode(token);
                return token;
            }
            start = BaseStream.Position - 1;
            if (MatchQNAN(currentChar))
            {
                var token = new Token { Type = TokenType.QNAN, Start = start, Length = BaseStream.Position - start };
                token.HashCode = GetTokenHashCode(token);
                return token;
            }
            start = BaseStream.Position - 1;
            if (MatchChar(currentChar))
            {
                var token = new Token { Type = TokenType.Character, Start = start, Length = BaseStream.Position - start };
                token.HashCode = GetTokenHashCode(token);
                return token;
            }
            return default;
        }

        protected string GetTokenValue(Token token)
        {
            var position = BaseStream.Position;
            BaseStream.Position = token.Start;
            int i;
            for (i = 0; i < token.Length; i++)
            {
                if (i >= _pool.Length)
                {
                    break;
                }
                _pool[i] = (byte)BaseStream.ReadByte();
            }
            BaseStream.Position = position;
            var value = Encoding.UTF8.GetString(_pool, 0, i);
            return value;
        }

        private long GetTokenHashCode(Token token)
        {
            var hash = HashUtils.GetHashInitialValue();
            var position = BaseStream.Position;
            BaseStream.Position = token.Start;
            for (var i = 0; i < token.Length; i++)
            {
                var b = BaseStream.ReadByte();
                hash = HashUtils.GetHash(hash, b);
            }
            BaseStream.Position = position;
            return hash;
        }

        private bool MatchChar(char currentChar)
        {
            var position = BaseStream.Position;
            if (EndOfStream || !char.IsLetter(currentChar))
            {
                BaseStream.Position = position;
                return false;
            }
            RollbackChar();
            return true;
        }

        private bool MatchIdentifier(char currentChar)
        {
            var position = BaseStream.Position;
            if (EndOfStream || (!char.IsLetter(currentChar) && currentChar != '_'))
            {
                BaseStream.Position = position;
                return false;
            }
            while (!EndOfStream && (char.IsNumber(currentChar) || char.IsLetter(currentChar) || currentChar == '_' || currentChar == '-'))
            {
                currentChar = ReadNextChar();
            }
            RollbackChar();
            return true;
        }

        private bool MatchInteger(char currentChar)
        {
            var position = BaseStream.Position;
            if (EndOfStream || (currentChar != '+' && currentChar != '-' && !char.IsNumber(currentChar)))
            {
                BaseStream.Position = position;
                return false;
            }
            do
            {
                currentChar = ReadNextChar();
            } while (!EndOfStream && char.IsNumber(currentChar));
            RollbackChar();
            return true;
        }

        private bool MatchFloat(char currentChar)
        {
            var position = BaseStream.Position;
            bool hasDecimal = false, hasExponent = false;
            if (EndOfStream || (currentChar != '+' && currentChar != '-' && !char.IsNumber(currentChar) && currentChar != '.'))
            {
                BaseStream.Position = position;
                return false;
            }
            currentChar = ReadNextChar();
            while (!EndOfStream && (char.IsNumber(currentChar) || currentChar == '.' || currentChar == 'e' || currentChar == 'E' || currentChar == '+' || currentChar == '-'))
            {
                if (currentChar == '.')
                {
                    if (hasDecimal) 
                    {
                        BaseStream.Position = position;
                        return false;
                    }
                    hasDecimal = true;
                    currentChar = ReadNextChar();
                }
                else if (currentChar == 'e' || currentChar == 'E')
                {
                    if (hasExponent) 
                    {
                        BaseStream.Position = position;
                        return false;
                    }
                    hasExponent = true;
                    hasDecimal = true; 

                    currentChar = ReadNextChar();
                    if (currentChar == '+' || currentChar == '-')
                    {
                        currentChar = ReadNextChar();
                    }
                }
                else
                {
                    currentChar = ReadNextChar();
                }
            }
            if (hasDecimal)
            {
                RollbackChar();
                return true;
            }
            BaseStream.Position = position;
            return false;
        }

        private void RollbackChar()
        {
            BaseStream.Position = _lastPosition;
        }

        private bool MatchQNAN(char currentChar)
        {
            var position = BaseStream.Position;
            if (EndOfStream || currentChar != '#')
            {
                BaseStream.Position = position;
                return false;
            }
            currentChar = ReadNextChar();
            if (EndOfStream || currentChar != 'Q')
            {
                BaseStream.Position = position;
                return false;
            }
            currentChar = ReadNextChar();
            if (EndOfStream || currentChar != 'N')
            {
                BaseStream.Position = position;
                return false;
            }
            currentChar = ReadNextChar();
            if (EndOfStream || currentChar != 'A')
            {
                BaseStream.Position = position;
                return false;
            }
            currentChar = ReadNextChar();
            if (EndOfStream || currentChar != 'N')
            {
                BaseStream.Position = position;
                return false;
            }
            return true;
        }

        public struct Token
        {
            public TokenType Type;
            public long Start;
            public long Length;
            public long HashCode;
        }
    }
}