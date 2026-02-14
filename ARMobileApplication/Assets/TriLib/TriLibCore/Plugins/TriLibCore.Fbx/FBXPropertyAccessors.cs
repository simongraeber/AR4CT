using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class PropertyAccessorIEnumerator<T> : IEnumerator<T> where T : struct
    {
        private readonly IList<T> _list;
        private int _listIndex;

        public PropertyAccessorIEnumerator(IList<T> list)
        {
            _list = list;
            _listIndex = -1;
        }

        public void Dispose()
        {
            
        }

        public bool MoveNext()
        {
            return ++_listIndex < _list.Count;
        }

        public void Reset()
        {
            _listIndex = -1;
        }

        public T Current => _list[_listIndex];

        object IEnumerator.Current => Current;
    }

    public class PropertyAccessorByte : IList<byte>
    {
        private FBXProperties _properties;
        private int _count;

        public PropertyAccessorByte(FBXProperties properties)
        {
            _count = properties.GetPropertyArrayLength();
            _properties = properties;
            IsReadOnly = true;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return new PropertyAccessorIEnumerator<byte>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(byte item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(byte item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            for (var i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = GetElement(i);
            }
        }

        public bool Remove(byte item)
        {
            throw new NotImplementedException();
        }

        public int Count => _count;
        public bool IsReadOnly { get; }

        public int IndexOf(byte item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, byte item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public byte this[int index]
        {
            get => GetElement(index);
            set => throw new NotImplementedException();
        }

        private byte GetElement(int i)
        {
            if (i < 0 || i >= Count)
            {
                return default;
            }
            if (_properties.IsASCII)
            {
                return _properties.ASCIIGetByteValue(i);
            }
            var activeProperty = _properties.Processor.LoadArrayProperty(_properties, ref _properties.Decoded, ref _properties.DecodedMemoryStream, ref _properties.DecodedBinaryReader, out var propertyType, out var encoded);
            _properties.Processor.ActiveBinaryReader.BaseStream.Seek(i * _properties.Processor.ActiveSubDataSize, SeekOrigin.Current);
            var value = _properties.BinaryConvertByteValue(_properties.Processor.ActiveBinaryReader, activeProperty, propertyType);
            _properties.Processor.ReleaseActiveBinaryReader();
            return value;
        }
    }

    public class PropertyAccessorShort : IList<short>
    {
        private FBXProperties _properties;
        private int _count;

        public PropertyAccessorShort(FBXProperties properties)
        {
            _count = properties.GetPropertyArrayLength();
            _properties = properties;
            IsReadOnly = true;
        }

        public IEnumerator<short> GetEnumerator()
        {
            return new PropertyAccessorIEnumerator<short>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(short item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(short item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(short[] array, int arrayIndex)
        {
            for (var i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = GetElement(i);
            }
        }

        public bool Remove(short item)
        {
            throw new NotImplementedException();
        }

        public int Count => _count;
        public bool IsReadOnly { get; }

        public int IndexOf(short item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, short item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public short this[int index]
        {
            get => GetElement(index);
            set => throw new NotImplementedException();
        }

        private short GetElement(int i)
        {
            if (i < 0 || i >= Count)
            {
                return default;
            }
            if (_properties.IsASCII)
            {
                return _properties.ASCIIGetShortValue(i);
            }
            else
            {

                var activeProperty = _properties.Processor.LoadArrayProperty(_properties, ref _properties.Decoded, ref _properties.DecodedMemoryStream, ref _properties.DecodedBinaryReader, out var propertyType, out var encoded);
                _properties.Processor.ActiveBinaryReader.BaseStream.Seek(i * _properties.Processor.ActiveSubDataSize, SeekOrigin.Current);
                var value = _properties.BinaryConvertShortValue(_properties.Processor.ActiveBinaryReader, activeProperty, propertyType);
                _properties.Processor.ReleaseActiveBinaryReader();
                return value;
            }
        }
    }

    public class PropertyAccessorIEE754 : IList<float>
    {
        private FBXProperties _properties;

        private readonly byte[] _bytesInt;
        private int _count;

        private unsafe byte[] GetBytesInt(int value, int offset)
        {
            var array = _bytesInt;
            offset = Mathf.Clamp(offset, 0, _bytesInt.Length-1);
            fixed (byte* ptr = &array[offset])
            {
                *(int*)ptr = value;
            }
            return array;
        }

        public PropertyAccessorIEE754(FBXProperties properties)
        {
            _count = properties.GetPropertyArrayLength();
           _properties = properties;
            _bytesInt = new byte[sizeof(int)];
            IsReadOnly = true;
        }

        public IEnumerator<float> GetEnumerator()
        {
            return new PropertyAccessorIEnumerator<float>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(float item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(float item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(float[] array, int arrayIndex)
        {
            for (var i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = GetElement(i);
            }
        }

        public bool Remove(float item)
        {
            throw new NotImplementedException();
        }

        public int Count => _count;
        public bool IsReadOnly { get; }

        public int IndexOf(float item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, float item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public float this[int index]
        {
            get => GetElement(index);
            set => throw new NotImplementedException();
        }

        private float GetElement(int i)
        {
            unsafe
            {
                if (i < 0 || i >= Count)
                {
                    return default;
                }
                int intValue;
                if (_properties.IsASCII)
                {
                    intValue = _properties.ASCIIGetIntValue(i);
                }
                else
                {
                    var activeProperty = _properties.Processor.LoadArrayProperty(_properties, ref _properties.Decoded, ref _properties.DecodedMemoryStream, ref _properties.DecodedBinaryReader, out var propertyType, out var encoded);
                    _properties.Processor.ActiveBinaryReader.BaseStream.Seek(i * _properties.Processor.ActiveSubDataSize, SeekOrigin.Current);
                    intValue = _properties.Processor.ActiveBinaryReader.ReadInt32();
                    _properties.Processor.ReleaseActiveBinaryReader();
                }
                var value = *(float*)&intValue;
                if (float.IsNaN(value))
                {
                    value = 0f;
                }
                return value;
            }
        }
    }

    public class PropertyAccessorInt : IList<int>
    {
        private FBXProperties _properties;
        private int _count;

        public PropertyAccessorInt(FBXProperties properties)
        {
            _count = properties.GetPropertyArrayLength();
            _properties = properties;
            IsReadOnly = true;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return new PropertyAccessorIEnumerator<int>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(int item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(int item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            for (var i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = GetElement(i);
            }
        }

        public bool Remove(int item)
        {
            throw new NotImplementedException();
        }

        public int Count => _count;
        public bool IsReadOnly { get; }

        public int IndexOf(int item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, int item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public int this[int index]
        {
            get => GetElement(index);
            set => throw new NotImplementedException();
        }

        private int GetElement(int i)
        {
            if (i < 0 || i >= Count)
            {
                return default;
            }
            if (_properties.IsASCII)
            {
                return _properties.ASCIIGetIntValue(i);
            }
            else
            {

                var activeProperty = _properties.Processor.LoadArrayProperty(_properties, ref _properties.Decoded, ref _properties.DecodedMemoryStream, ref _properties.DecodedBinaryReader, out var propertyType, out var encoded);
                 _properties.Processor.ActiveBinaryReader.BaseStream.Seek(i * _properties.Processor.ActiveSubDataSize, SeekOrigin.Current);
                var value = _properties.BinaryConvertIntValue(_properties.Processor.ActiveBinaryReader, activeProperty, propertyType);
                _properties.Processor.ReleaseActiveBinaryReader();
                return value;
            }
        }
    }

    public class PropertyAccessorLong : IList<long>
    {
        private FBXProperties _properties;
        private bool _isTime;
        private int _count;

        public PropertyAccessorLong(FBXProperties properties, bool isTime)
        {
            _count = properties.GetPropertyArrayLength();
            _properties = properties;
            _isTime = isTime;
            IsReadOnly = true;
        }

        public IEnumerator<long> GetEnumerator()
        {
            return new PropertyAccessorIEnumerator<long>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(long item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(long item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(long[] array, int arrayIndex)
        {
            for (var i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = GetElement(i);
            }
        }

        public bool Remove(long item)
        {
            throw new NotImplementedException();
        }

        public int Count => _count;
        public bool IsReadOnly { get; }

        public int IndexOf(long item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, long item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public long this[int index]
        {
            get => GetElement(index);
            set => throw new NotImplementedException();
        }

        private long GetElement(int i)
        {
            if (i < 0 || i >= Count)
            {
                return default;
            }
            if (_properties.IsASCII)
            {
                var value = _properties.ASCIIGetLongValue(i);
                if (_isTime)
                {
                    var converted = _properties.Processor.Document.ConvertFromFBXTime(value);
                    if (converted > 100000f)
                    {
                        return _properties.Processor.Document.ConvertToFBXTime(100000f);
                    }
                    else if (converted < -100000f)
                    {
                        return _properties.Processor.Document.ConvertToFBXTime(-100000f);
                    }
                }
                return value;
            }
            else
            {
                var activeProperty = _properties.Processor.LoadArrayProperty(_properties, ref _properties.Decoded, ref _properties.DecodedMemoryStream, ref _properties.DecodedBinaryReader, out var propertyType, out var encoded);
                _properties.Processor.ActiveBinaryReader.BaseStream.Seek(i * _properties.Processor.ActiveSubDataSize, SeekOrigin.Current);
                var value = _properties.BinaryConvertLongValue(_properties.Processor.ActiveBinaryReader, activeProperty, propertyType);
                _properties.Processor.ReleaseActiveBinaryReader();
                if (_isTime)
                {
                    var converted = _properties.Processor.Document.ConvertFromFBXTime(value);
                    if (converted > 100000f)
                    {
                        return _properties.Processor.Document.ConvertToFBXTime(100000f);
                    }
                    else if (converted < -100000f)
                    {
                        return _properties.Processor.Document.ConvertToFBXTime(-100000f);
                    }
                }
                return value;
            }
        }
    }

    public class PropertyAccessorFloat : IList<float>
    {
        private FBXProperties _properties;
        private int _count;

        public PropertyAccessorFloat(FBXProperties properties)
        {
            _count = properties.GetPropertyArrayLength();
            _properties = properties;
            IsReadOnly = true;
        }

        public IEnumerator<float> GetEnumerator()
        {
            return new PropertyAccessorIEnumerator<float>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(float item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(float item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(float[] array, int arrayIndex)
        {
            for (var i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = GetElement(i);
            }
        }

        public bool Remove(float item)
        {
            throw new NotImplementedException();
        }

        public int Count => _count;
        public bool IsReadOnly { get; }

        public int IndexOf(float item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, float item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public float this[int index]
        {
            get => GetElement(index);
            set => throw new NotImplementedException();
        }

        private float GetElement(int i)
        {
            if (i < 0 || i >= Count)
            {
                return default;
            }
            if (_properties.IsASCII)
            {
                return _properties.ASCIIGetFloatValue(i);
            }
            else
            {

                var activeProperty = _properties.Processor.LoadArrayProperty(_properties, ref _properties.Decoded, ref _properties.DecodedMemoryStream, ref _properties.DecodedBinaryReader, out var propertyType, out var encoded);
                _properties.Processor.ActiveBinaryReader.BaseStream.Seek(i * _properties.Processor.ActiveSubDataSize, SeekOrigin.Current);
                var value = _properties.BinaryConvertFloatValue(_properties.Processor.ActiveBinaryReader, activeProperty, propertyType);
                _properties.Processor.ReleaseActiveBinaryReader();
                return value;
            }
        }
    }

    public class PropertyAccessorDouble : IList<double>
    {
        private FBXProperties _properties;
        private int _count;

        public PropertyAccessorDouble(FBXProperties properties)
        {
            _count = properties.GetPropertyArrayLength();
            _properties = properties;
            IsReadOnly = true;
        }

        public IEnumerator<double> GetEnumerator()
        {
            return new PropertyAccessorIEnumerator<double>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(double item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(double item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(double[] array, int arrayIndex)
        {
            for (var i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = GetElement(i);
            }
        }

        public bool Remove(double item)
        {
            throw new NotImplementedException();
        }

        public int Count => _count;
        public bool IsReadOnly { get; }

        public int IndexOf(double item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, double item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public double this[int index]
        {
            get => GetElement(index);
            set => throw new NotImplementedException();
        }

        private double GetElement(int i)
        {
            if (i < 0 || i >= Count)
            {
                return default;
            }
            if (_properties.IsASCII)
            {
                return _properties.ASCIIGetDoubleValue(i);
            }
            else
            {

                var activeProperty = _properties.Processor.LoadArrayProperty(_properties, ref _properties.Decoded, ref _properties.DecodedMemoryStream, ref _properties.DecodedBinaryReader, out var propertyType, out var encoded);
                _properties.Processor.ActiveBinaryReader.BaseStream.Seek(i * _properties.Processor.ActiveSubDataSize, SeekOrigin.Current);
                var value = _properties.BinaryConvertDoubleValue(_properties.Processor.ActiveBinaryReader, activeProperty, propertyType);
                _properties.Processor.ReleaseActiveBinaryReader();
                return value;
            }
        }
    }
}