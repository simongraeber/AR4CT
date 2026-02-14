using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TriLibCore.Fbx.Reader;
using TriLibCore.Utils;

namespace TriLibCore.Fbx.ASCII
{
    public class FBXASCIIReader : FBXASCIITokenizer
    {
        private readonly FBXProcessor _processor;
        private Token _currentToken;

        private readonly byte[] _pool = new byte[256];

        public FBXASCIIReader(FBXProcessor processor, Stream stream, bool leaveOpen) : base(stream, leaveOpen)
        {
            _processor = processor;
        }

        public string GetPropertyStringValue(FBXProperty property, bool isNumber, bool hashOnly)
        {
            var position = BaseStream.Position;
            BaseStream.Position = property.Position;
            if (!hashOnly)
            {
                string result;
                if (property.StringCharLength <= _pool.Length)
                {
                    var read = Read(_pool, 0, property.StringCharLength);
                    result = Encoding.UTF8.GetString(_pool, 0, read);
                }
                else
                {
                    var data = new byte[property.StringCharLength];
                    var read = Read(data, 0, property.StringCharLength);
                    result = Encoding.UTF8.GetString(data, 0, read);
                }
                BaseStream.Position = position;
                return result;
            }
            var hash = HashUtils.GetHashInitialValue();
            for (var i = 0; i < property.StringCharLength; i++)
            {
                hash = HashUtils.GetHash(hash, BaseStream.ReadByte());
            }
            CharHash = hash;
            BaseStream.Position = position;
            return null;
        }

        public IEnumerator<byte> GetPropertyStringValueEnumerator(FBXProperty property)
        {
            BaseStream.Position = property.Position;
            return new ASCIIValueEnumerator(this);
        }

        public FBXNode ParseDocument()
        {
            if (_currentToken.Type != TokenType.EOF)
            {
                var root = new FBXNode("FBXRoot");
                ParseNodeList(root, 0);
                return root;
            }
            return null;
        }

        public FBXNode ReadASCIIDocument()
        {
            _processor.ActiveASCIIReader = this;
            _currentToken = ReadToken();
            var rootNode = ParseDocument();
            return rootNode;
        }

        private void Consume(TokenType expectedType)
        {
            if (_currentToken.Type == expectedType)
            {
                _currentToken = ReadToken();
            }
            else
            {
                throw new InvalidOperationException($"Expected token type {expectedType}, but found {_currentToken.Type}");
            }
        }

        private void ParseArray(FBXNode parentNode, int indent)
        {
            var node = new FBXNode(_currentToken.HashCode);
            Consume(TokenType.Identifier);
            Consume(TokenType.Colon);
            Consume(TokenType.Asterisk);
            var arrayLengthString = GetTokenValue(_currentToken);
            var arrayLength = int.Parse(arrayLengthString);
            node.Properties = new FBXProperties(_processor, arrayLength);
            Consume(TokenType.Integer);
            Consume(TokenType.LeftBrace);
            if (_currentToken.Type == TokenType.Identifier && PeekToken().Type == TokenType.Colon)
            {
                Consume(TokenType.Identifier);
                Consume(TokenType.Colon);
                ParsePropertiesList(node);
            }
            Consume(TokenType.RightBrace);
            parentNode.Add(node);
        }

        private void ParseBase64Node(FBXNode parentNode, int indent)
        {
            void AddProperty()
            {
                var property = new FBXProperty();
                property.Position = _currentToken.Start;
                property.StringCharLength = (int)_currentToken.Length;
                parentNode.Properties.Values.Add(property);
                parentNode.Properties.ArrayLength++;
            }
            Consume(TokenType.Comma);
            parentNode.Properties = new FBXProperties(_processor, 0);
            AddProperty();
            Consume(TokenType.String);
            var peekComma = _currentToken;
            while (peekComma.Type == TokenType.Comma)
            {
                Consume(TokenType.Comma);
                var peekString = _currentToken;
                if (peekString.Type == TokenType.String)
                {
                    AddProperty();
                    Consume(TokenType.String);
                    peekComma = _currentToken;
                }
            }
        }

        private void ParseNode(FBXNode parentNode, int indent)
        {
            var node = new FBXNode(_currentToken.HashCode);
            Consume(TokenType.Identifier);
            Consume(TokenType.Colon);
            var peekToken = PeekToken(0);
            if (_currentToken.Type != TokenType.Identifier || peekToken.Type != TokenType.Colon)
            {
                if (_currentToken.Type == TokenType.Comma && peekToken.Type == TokenType.String)
                {
                    ParseBase64Node(node, ++indent);
                }
                else if (_currentToken.Type != TokenType.LeftBrace)
                {
                    ParsePropertiesList(node);
                }

                if (_currentToken.Type == TokenType.LeftBrace)
                {
                    Consume(TokenType.LeftBrace);
                    ParseNodeList(node, ++indent);
                    Consume(TokenType.RightBrace);
                }
            }
            parentNode.Add(node);
        }

        private void ParseNodeList(FBXNode parentNode, int indent)
        {
            while (_currentToken.Type != TokenType.EOF && _currentToken.Type != TokenType.RightBrace)
            {
                if (_currentToken.Type == TokenType.Identifier && PeekToken().Type == TokenType.Colon && PeekToken(1).Type == TokenType.Asterisk)
                {
                    ParseArray(parentNode, indent);
                }
                else
                {
                    ParseNode(parentNode, indent);
                }
            }
        }

        private void ParsePropertiesList(FBXNode node)
        {
            if (node.Properties == null)
            {
                node.Properties = new FBXProperties(_processor, 1);
            }
            while (_currentToken.Type != TokenType.EOF && _currentToken.Type != TokenType.LeftBrace && _currentToken.Type != TokenType.RightBrace)
            {
                var property = new FBXProperty();
                property.Position = _currentToken.Start;
                property.StringCharLength = (int)_currentToken.Length;
                node.Properties.Values.Add(property);
                _currentToken = ReadToken();
                if (_currentToken.Type == TokenType.Comma)
                {
                    Consume(TokenType.Comma);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
