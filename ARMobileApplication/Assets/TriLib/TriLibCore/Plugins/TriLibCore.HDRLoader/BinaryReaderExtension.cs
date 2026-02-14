#pragma warning disable 168
using System.IO;
using System.Text;

namespace HDRLoader
{
    internal static class BinaryReaderExtension
    {
        public static string ReadLine(this BinaryReader reader)
        {
            var result = new StringBuilder();
            bool foundEndOfLine = false;
            char ch;
            while (!foundEndOfLine)
            {
                try
                {
                    ch = reader.ReadChar();
                }
                catch (EndOfStreamException ex)
                {
                    if (result.Length == 0) return null;
                    else break;
                }

                switch (ch)
                {
                    case '\r':
                        if (reader.PeekChar() == '\n') reader.ReadChar();
                        foundEndOfLine = true;
                        break;
                    case '\n':
                        foundEndOfLine = true;
                        break;
                    default:
                        result.Append(ch);
                        break;
                }
            }
            return result.ToString();
        }

        public static void ReadFully(this BinaryReader stream, byte[] buffer, int offset = 0, int? expected = null)
        {
            if (!expected.HasValue)
            {
                expected = buffer.Length;
            }
            var readBytes = stream.Read(buffer, offset, expected.Value);
            if (readBytes != expected.Value)
            {
                throw new EndOfStreamException();
            }
        }
    }
}