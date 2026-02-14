using System;
using System.IO;
using System.Text;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides methods for reading primitive data types as big-endian values from a stream.
    /// </summary>
    public class BigEndianBinaryReader : BinaryReader
    {
        private readonly byte[] _data2 = new byte[2];
        private readonly byte[] _data4 = new byte[4];
        private readonly byte[] _data8 = new byte[8];

        /// <summary>
        /// Initializes a new instance of the <see cref="BigEndianBinaryReader"/> class based on the specified stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        public BigEndianBinaryReader(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BigEndianBinaryReader"/> class based on the specified stream and character encoding, and optionally leaves the stream open.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="encoding">The character encoding.</param>
        /// <param name="leaveOpen">true to leave the stream open after the <see cref="BigEndianBinaryReader"/> object is disposed; otherwise, false.</param>
        public BigEndianBinaryReader(Stream stream, Encoding encoding, bool leaveOpen)
            : base(stream, encoding, leaveOpen)
        {
        }

        /// <summary>
        /// Reads a 4-byte signed integer from the current stream using big-endian encoding.
        /// </summary>
        /// <returns>A 4-byte signed integer read from the current stream.</returns>
        public override int ReadInt32()
        {
            Array.Clear(_data4, 0, 4);
            base.Read(_data4, 0, 4);
            Array.Reverse(_data4);
            return BitConverter.ToInt32(_data4, 0);
        }

        /// <summary>
        /// Reads a 2-byte signed integer from the current stream using big-endian encoding.
        /// </summary>
        /// <returns>A 2-byte signed integer read from the current stream.</returns>
        public override short ReadInt16()
        {
            Array.Clear(_data2, 0, 2);
            base.Read(_data2, 0, 2);
            Array.Reverse(_data2);
            return BitConverter.ToInt16(_data2, 0);
        }

        /// <summary>
        /// Reads an 8-byte signed integer from the current stream using big-endian encoding.
        /// </summary>
        /// <returns>An 8-byte signed integer read from the current stream.</returns>
        public override long ReadInt64()
        {
            Array.Clear(_data8, 0, 8);
            base.Read(_data8, 0, 8);
            Array.Reverse(_data8);
            return BitConverter.ToInt64(_data8, 0);
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream using big-endian encoding.
        /// </summary>
        /// <returns>A 4-byte unsigned integer read from the current stream.</returns>
        public override uint ReadUInt32()
        {
            Array.Clear(_data4, 0, 4);
            base.Read(_data4, 0, 4);
            Array.Reverse(_data4);
            return BitConverter.ToUInt32(_data4, 0);
        }

        /// <summary>
        /// Reads a 4-byte floating-point value from the current stream using big-endian encoding.
        /// </summary>
        /// <returns>A 4-byte floating-point value read from the current stream.</returns>
        public override float ReadSingle()
        {
            Array.Clear(_data4, 0, 4);
            base.Read(_data4, 0, 4);
            Array.Reverse(_data4);
            return BitConverter.ToSingle(_data4, 0);
        }

        /// <summary>
        /// Reads an 8-byte floating-point value from the current stream using big-endian encoding.
        /// </summary>
        /// <returns>An 8-byte floating-point value read from the current stream.</returns>
        public override double ReadDouble()
        {
            Array.Clear(_data8, 0, 8);
            base.Read(_data8, 0, 8);
            Array.Reverse(_data8);
            return BitConverter.ToDouble(_data8, 0);
        }
    }
}
