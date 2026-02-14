using TriLibCore.Utils;
using Unity.Collections.LowLevel.Unsafe;

namespace TriLibCore.Gltf
{
    public struct TemporaryString
    {
        private char[] _chars;
        private string _charString;
        private readonly int _length;

        public TemporaryString(int length)
        {
            _charString = new string('\0', length);
            _chars = new char[length];
            _length = length;
        }

        public string GetString(JsonParser.JsonValue value)
        {
            unsafe
            {
                fixed (char* c = _chars)
                {
                    var length = _length  * sizeof(char);
                    UnsafeUtility.MemSet(c, 0, length);
                    value.CopyTo(_chars);
                    fixed (char* s = _charString)
                    {
                        UnsafeUtility.MemCpy(s, c, length);
                    }
                }
            }
            return _charString;
        }
    }
}