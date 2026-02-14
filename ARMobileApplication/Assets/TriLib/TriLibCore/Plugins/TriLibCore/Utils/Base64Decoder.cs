using System;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides functionality to decode data from a Base64-encoded source one byte at a time.
    /// </summary>
    public class Base64Decoder
    {
        // A lookup table for decoding Base64 characters into their 6-bit values.
        private static readonly byte[] Base64DecodeMap = new byte[] {
            // 0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 0
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 1
            255,255,255,255,255,255,255,255,255,255,255, 62,255,255,255, 63, // 2
            52, 53, 54, 55, 56, 57, 58, 59, 60, 61,255,255,255,255,255,255, // 3
            255,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, // 4
            15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25,255,255,255,255,255, // 5
            255, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, // 6
            41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51,255,255,255,255,255, // 7
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 8
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 9
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // A
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // B
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // C
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // D
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // E
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // F
        };

        // Represents a state used while reading and decoding Base64 data.
        private class ReadStateInfo
        {
            /// <summary>
            /// Holds the intermediate value between decoding steps.
            /// </summary>
            public byte Val { get; set; }

            /// <summary>
            /// Indicates the current position in the decoding sequence.
            /// Valid positions are 0, 1, 2, and 3.
            /// </summary>
            public byte Pos { get; set; }
        }

        // Maintains the state for the current decode operation.
        private ReadStateInfo _readState;

        // Specifies the value used to identify invalid Base64 characters.
        private const byte InvalidBase64Value = 255;

        // Used internally for temporary storage, if needed.
        private readonly byte[] _temp = new byte[4];

        /// <summary>
        /// Gets the read state for this <see cref="Base64Decoder"/>, creating a new instance if none exists.
        /// </summary>
        private ReadStateInfo ReadState => _readState ?? (_readState = new ReadStateInfo());

        /// <summary>
        /// Decodes a single Base64 character (in byte form) and produces an output byte, if enough data has been gathered.
        /// </summary>
        /// <param name="data">
        /// The byte representing a Base64 character. Whitespace and certain control characters
        /// (such as <c>'='</c>, <c>'\r'</c>, <c>'\n'</c>) are ignored.
        /// </param>
        /// <param name="output">
        /// When this method returns <see langword="true"/>, contains the decoded byte.
        /// When this method returns <see langword="false"/>, the <paramref name="output"/> is not yet defined.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a decoded byte is produced by this method; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown if the specified <paramref name="data"/> is an invalid Base64 character.
        /// </exception>
        public bool DecodeByte(byte data, out byte output)
        {
            output = default;
            // Ignore certain characters (whitespace, padding, etc.)
            if (data == '\r' || data == '\n' || data == '=' || data == ' ' || data == '\t')
            {
                return false;
            }

            var s = Base64DecodeMap[data];
            if (s == InvalidBase64Value)
            {
                throw new FormatException("Base64InvalidCharacter");
            }

            switch (ReadState.Pos)
            {
                case 0:
                    ReadState.Val = (byte)(s << 2);
                    ReadState.Pos++;
                    return false;
                case 1:
                    output = (byte)(ReadState.Val + (s >> 4));
                    ReadState.Val = (byte)(s << 4);
                    ReadState.Pos++;
                    break;
                case 2:
                    output = (byte)(ReadState.Val + (s >> 2));
                    ReadState.Val = (byte)(s << 6);
                    ReadState.Pos++;
                    break;
                case 3:
                    output = (byte)(ReadState.Val + s);
                    ReadState.Pos = 0;
                    break;
            }

            return true;
        }
    }
}
