using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace TriLibCore.Fbx.ASCII
{
    public struct ASCIIValueEnumerator : IEnumerator<byte>
    {
        private readonly BinaryReader _reader;

        public ASCIIValueEnumerator(FBXASCIIReader reader)
        {
            _reader = reader;
            Current = default;
        }

        public void Dispose()
        {

        }

        public bool MoveNext()
        {
            Current = _reader.ReadByte();
            return Current != '"';
        }

        public void Reset()
        {

        }

        public byte Current { get; private set; }

        object IEnumerator.Current => Current;
    }
}