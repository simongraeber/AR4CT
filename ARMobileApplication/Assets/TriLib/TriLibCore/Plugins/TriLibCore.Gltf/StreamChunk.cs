using System;
using System.IO;

namespace TriLibCore.Gltf
{
    public class StreamChunk : Stream
    {
        private int _relativePosition;

        public StreamChunk(Stream baseStream, int length = -1, int offset = 0)
        {
            Offset = (int)(baseStream.Position  + offset);
            BasePosition = Offset;
            while (baseStream is StreamChunk streamChunk)
            {
                BasePosition += streamChunk.Offset;
                baseStream = streamChunk.BaseStream;
            }
            BaseStream = baseStream;
            Length = baseStream == null || length > -1 ? length : baseStream.Length;
            _relativePosition = 0;
        }

        public byte this[int index]
        {
            get
            {
                if (BaseStream == null)
                {
                    return 0;
                }

                var baseStreamPosition = BaseStream.Position;
                BaseStream.Position = BasePosition + index;
                var value = BaseStream.ReadByte();
                if (value < 0)
                {
                    value = 0;
                }

                BaseStream.Position = baseStreamPosition;
                return (byte)value;
            }
        }

        public int Offset { get; }

        public int BasePosition { get; }

        public Stream BaseStream { get; }

        public override bool CanRead => BaseStream?.CanRead ?? false;
        public override bool CanSeek => BaseStream?.CanSeek ?? false;
        public override bool CanWrite => false;
        public override long Length { get; }

        public override long Position
        {
            get => _relativePosition;
            set => _relativePosition = (int)value;
        }


        public override void Flush()
        {
            BaseStream?.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _relativePosition = (int)offset;
                    break;
                case SeekOrigin.Current:
                    _relativePosition += (int)offset;
                    break;
                case SeekOrigin.End:
                    _relativePosition = (int)(Length - offset);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            return Position;
        }

        public override void SetLength(long value)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (BaseStream == null)
            {
                return 0;
            }
            BaseStream.Position = BasePosition + _relativePosition;
            var read = BaseStream.Read(buffer, offset, count);
            _relativePosition += read;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public byte[] ToBytes(byte[] bytes, int index)
        {
            if (BaseStream == null)
            {
                return null;
            }

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = 0;
            }

            var baseStreamPosition = BaseStream.Position;
            BaseStream.Position = BasePosition + index;
            BaseStream.Read(bytes, 0, bytes.Length);
            BaseStream.Position = baseStreamPosition;
            return bytes;
        }
    }
}