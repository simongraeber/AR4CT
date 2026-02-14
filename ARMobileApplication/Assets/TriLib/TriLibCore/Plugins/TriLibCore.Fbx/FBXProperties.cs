using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TriLibCore.Fbx.Reader;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXProperties
    {
        #region General
        public FBXProcessor Processor { get; }

        public bool IsASCII => Processor.FBXBinaryReader == null;

        public int ArrayLength;

        public byte[] Decoded;
        public MemoryStream DecodedMemoryStream;
        public BinaryReader DecodedBinaryReader;

        public readonly List<FBXProperty> Values;

        public FBXProperties(FBXProcessor processor)
        {
            Processor = processor;
            ArrayLength = 0;
        }

        public FBXProperties(FBXProcessor processor, int arrayLength)
        {
            Processor = processor;
            ArrayLength = arrayLength;
            Values = new List<FBXProperty>();
        }

        public long GetStringHashValue(int index)
        {
            if (IsASCII)
            {
                ASCIIGetStringValue(index, true);
                return Processor.ActiveASCIIReader.CharHash;
            }
            else
            {
                BinaryGetStringValue(index, true);
                return Processor.FBXBinaryReader.CharsHash;
            }
        }

        public string GetConvertedStringValue(int index)
        {
            if (IsASCII)
            {
                return "\"" + ASCIIGetStringValue(index, false) + "\"";
            }
            var binaryProperty = Values[index];
            binaryProperty.GetPropertyData(this, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
            switch (propertyType)
            {
                case 'R':
                case 'C':
                case 'c':
                    {
                        var value = Processor.ActiveBinaryReader.ReadByte();
                        return "c:" + "\"" + value + "\"";
                    }
                case 'Y':
                case 'y':
                    {
                        var value = Processor.ActiveBinaryReader.ReadInt16();
                        return "y:" + value.ToString();
                    }
                case 'B':
                case 'b':
                    {
                        var value = Processor.ActiveBinaryReader.ReadInt32();
                        return "b:" + value.ToString();
                    }
                case 'I':
                case 'i':
                    {
                        var value = Processor.ActiveBinaryReader.ReadInt32();
                        return "i:" + value.ToString();
                    }
                case 'L':
                case 'l':
                    {
                        var value = Processor.ActiveBinaryReader.ReadInt64();
                        return "l:" + value.ToString();
                    }
                case 'F':
                case 'f':
                    {
                        var value = Processor.ActiveBinaryReader.ReadSingle();
                        return "f:" + value.ToString(CultureInfo.InvariantCulture);
                    }
                case 'D':
                case 'd':
                    {
                        var value = Processor.ActiveBinaryReader.ReadDouble();
                        return "d:" + value.ToString(CultureInfo.InvariantCulture);
                    }
                default:
                    {
                        var value = BinaryGetStringValue(index, false);
                        if (value != null)
                        {
                            return "\"" + value + "\"";
                        }
                        break;
                    }
            }
            return "unknown";
        }

        public string GetStringValue(int index, bool dummy)
        {
            return IsASCII ? ASCIIGetStringValue(index, false) : BinaryGetStringValue(index, false);
        }

        public byte GetByteValue(int index)
        {
            return IsASCII ? ASCIIGetByteValue(index) : BinaryGetByteValue(index);
        }

        public short GetShortValue(int index)
        {
            return IsASCII ? ASCIIGetShortValue(index) : BinaryGetShortValue(index);
        }

        public int GetIntValue(int index)
        {
            return IsASCII ? ASCIIGetIntValue(index) : BinaryGetIntValue(index);
        }

        public long GetLongValue(int index)
        {
            return IsASCII ? ASCIIGetLongValue(index) : BinaryGetLongValue(index);
        }

        public float GetFloatValue(int index)
        {
            return IsASCII ? ASCIIGetFloatValue(index) : BinaryGetFloatValue(index);
        }

        public double GetDoubleValue(int index)
        {
            return IsASCII ? ASCIIGetDoubleValue(index) : BinaryGetDoubleValue(index);
        }

        public bool GetBoolValue(int index)
        {
            return IsASCII ? ASCIIGetBoolValue(index) : BinaryGetBoolValue(index);
        }

        public Vector2 GetVector2Value(int index)
        {
            return IsASCII ? ASCIIGetVector2Value(index) : BinaryGetVector2Value(index);
        }

        public Vector3 GetVector3Value(int index)
        {
            return IsASCII ? ASCIIGetVector3Value(index) : BinaryGetVector3Value(index);
        }

        public Vector4 GetVector4Value(int index)
        {
            return IsASCII ? ASCIIGetVector4Value(index) : BinaryGetVector4Value(index);
        }

        public Color GetColorValue(int index)
        {
            return IsASCII ? ASCIIGetColorValue(index) : BinaryGetColorValue(index);
        }

        public Color GetColorAlphaValue(int index)
        {
            return IsASCII ? ASCIIGetColorAlphaValue(index) : BinaryGetColorAlphaValue(index);
        }

        public Matrix4x4 GetMatrixValue()
        {
            return IsASCII ? ASCIIGetMatrixValue() : BinaryGetMatrixValue();
        }

        public int GetPropertyArrayLength()
        {
            return IsASCII ? ArrayLength : BinaryGetPropertyArrayLength();
        }
        #endregion

        #region Binary
        private string BinaryGetStringValue(int index, bool hashOnly)
        {
            var binaryProperty = Values[index];
            binaryProperty.GetPropertyData(this, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
            if (arrayLength <= 0)
            {
                return null;
            }
            if (arrayLength >= Processor.FBXBinaryReader.Buffer.Length)
            {
                throw new Exception("Invalid string");
            }
            Array.Clear(Processor.FBXBinaryReader.Buffer, 0, Processor.FBXBinaryReader.Buffer.Length);
            Array.Clear(Processor.FBXBinaryReader.Chars, 0, Processor.FBXBinaryReader.Chars.Length);
            Processor.FBXBinaryReader.BaseStream.Read(Processor.FBXBinaryReader.Buffer, 0, arrayLength);
            Encoding.UTF8.GetChars(Processor.FBXBinaryReader.Buffer, 0, arrayLength, Processor.FBXBinaryReader.Chars, 0);
            var charCount = 0;
            var zeroCount = 0;
            do
            {
                var currentChar = Processor.FBXBinaryReader.Chars[charCount++];
                zeroCount = currentChar == '\0' ? zeroCount + 1 : 0;
            } while (charCount < Processor.FBXBinaryReader.Buffer.Length && zeroCount < 2);
            if (hashOnly)
            {
                Processor.FBXBinaryReader.CharsHash = HashUtils.GetHash(Processor.FBXBinaryReader.Chars, charCount - 2);
                return null;
            }
            var value = new string(Processor.FBXBinaryReader.Chars, 0, charCount - 2);
            return value;
        }

        private byte BinaryGetByteValue(int index)
        {
            var binaryProperty = Values[index];
            Processor.FBXBinaryReader.BaseStream.Seek(binaryProperty.Position, SeekOrigin.Begin);
            binaryProperty.GetPropertyData(this, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
            return BinaryConvertByteValue(Processor.FBXBinaryReader, binaryProperty, propertyType);
        }

        public byte BinaryConvertByteValue(BinaryReader binaryReader, FBXProperty binaryProperty, char propertyType)
        {
            switch (propertyType)
            {
                case 'R':
                case 'C':
                case 'c':
                    {
                        var value = binaryReader.ReadByte();
                        return value;
                    }
                case 'Y':
                case 'y':
                    {
                        var value = binaryReader.ReadInt16();
                        return (byte)value;
                    }
                case 'B':
                case 'b':
                    {
                        var value = binaryReader.ReadInt32();
                        return (byte)value;
                    }
                case 'I':
                case 'i':
                    {
                        var value = binaryReader.ReadInt32();
                        return (byte)value;
                    }
                case 'L':
                case 'l':
                    {
                        var value = binaryReader.ReadInt64();
                        return (byte)value;
                    }
                case 'F':
                case 'f':
                    {
                        var value = binaryReader.ReadSingle();
                        return (byte)value;
                    }
                case 'D':
                case 'd':
                    {
                        var value = binaryReader.ReadDouble();
                        return (byte)value;
                    }
            }

            return default(byte);
        }

        private short BinaryGetShortValue(int index)
        {
            var binaryProperty = Values[index];
            Processor.FBXBinaryReader.BaseStream.Seek(binaryProperty.Position, SeekOrigin.Begin);
            binaryProperty.GetPropertyData(this, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
            return BinaryConvertShortValue(Processor.FBXBinaryReader, binaryProperty, propertyType);
        }

        public short BinaryConvertShortValue(BinaryReader binaryReader, FBXProperty binaryProperty, char propertyType)
        {
            switch (propertyType)
            {
                case 'R':
                case 'C':
                case 'c':
                    {
                        var value = binaryReader.ReadByte();
                        return value;
                    }
                case 'Y':
                case 'y':
                    {
                        var value = binaryReader.ReadInt16();
                        return value;
                    }
                case 'B':
                case 'b':
                    {
                        var value = binaryReader.ReadInt32();
                        return (short)value;
                    }
                case 'I':
                case 'i':
                    {
                        var value = binaryReader.ReadInt32();
                        return (short)value;
                    }
                case 'L':
                case 'l':
                    {
                        var value = binaryReader.ReadInt64();
                        return (short)value;
                    }
                case 'F':
                case 'f':
                    {
                        var value = binaryReader.ReadSingle();
                        return (short)value;
                    }
                case 'D':
                case 'd':
                    {
                        var value = binaryReader.ReadDouble();
                        return (short)value;
                    }
            }

            return default(short);
        }

        private int BinaryGetIntValue(int index)
        {
            var binaryProperty = Values[index];
            Processor.FBXBinaryReader.BaseStream.Seek(binaryProperty.Position, SeekOrigin.Begin);
            binaryProperty.GetPropertyData(this, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
            return BinaryConvertIntValue(Processor.FBXBinaryReader, binaryProperty, propertyType);
        }

        public int BinaryConvertIntValue(BinaryReader binaryReader, FBXProperty binaryProperty, char propertyType)
        {
            switch (propertyType)
            {
                case 'R':
                case 'C':
                case 'c':
                    {
                        var value = binaryReader.ReadByte();
                        return value;
                    }
                case 'Y':
                case 'y':
                    {
                        var value = binaryReader.ReadInt16();
                        return value;
                    }
                case 'B':
                case 'b':
                    {
                        var value = binaryReader.ReadInt32();
                        return value;
                    }
                case 'I':
                case 'i':
                    {
                        var value = binaryReader.ReadInt32();
                        return value;
                    }
                case 'L':
                case 'l':
                    {
                        var value = binaryReader.ReadInt64();
                        return (int)value;
                    }
                case 'F':
                case 'f':
                    {
                        var value = binaryReader.ReadSingle();
                        return (int)value;
                    }
                case 'D':
                case 'd':
                    {
                        var value = binaryReader.ReadDouble();
                        return (int)value;
                    }
            }

            return default(int);
        }

        private long BinaryGetLongValue(int index)
        {
            var binaryProperty = Values[index];
            Processor.FBXBinaryReader.BaseStream.Seek(binaryProperty.Position, SeekOrigin.Begin);
            binaryProperty.GetPropertyData(this, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
            return BinaryConvertLongValue(Processor.FBXBinaryReader, binaryProperty, propertyType);
        }

        public long BinaryConvertLongValue(BinaryReader binaryReader, FBXProperty binaryProperty, char propertyType)
        {
            switch (propertyType)
            {
                case 'R':
                case 'C':
                case 'c':
                    {
                        var value = binaryReader.ReadByte();
                        return value;
                    }
                case 'Y':
                case 'y':
                    {
                        var value = binaryReader.ReadInt16();
                        return value;
                    }
                case 'B':
                case 'b':
                    {
                        var value = binaryReader.ReadInt32();
                        return value;
                    }
                case 'I':
                case 'i':
                    {
                        var value = binaryReader.ReadInt32();
                        return value;
                    }
                case 'L':
                case 'l':
                    {
                        var value = binaryReader.ReadInt64();
                        return value;
                    }
                case 'F':
                case 'f':
                    {
                        var value = binaryReader.ReadSingle();
                        return (long)value;
                    }
                case 'D':
                case 'd':
                    {
                        var value = binaryReader.ReadDouble();
                        return (long)value;
                    }
            }

            return default(long);
        }

        private float BinaryGetFloatValue(int index)
        {
            var binaryProperty = Values[index];
            Processor.FBXBinaryReader.BaseStream.Seek(binaryProperty.Position, SeekOrigin.Begin);
            binaryProperty.GetPropertyData(this, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
            return BinaryConvertFloatValue(Processor.FBXBinaryReader, binaryProperty, propertyType);
        }

        public float BinaryConvertFloatValue(BinaryReader binaryReader, FBXProperty binaryProperty, char propertyType)
        {
            switch (propertyType)
            {
                case 'R':
                case 'C':
                case 'c':
                    {
                        var value = binaryReader.ReadByte();
                        return value;
                    }
                case 'Y':
                case 'y':
                    {
                        var value = binaryReader.ReadInt16();
                        return value;
                    }
                case 'B':
                case 'b':
                    {
                        var value = binaryReader.ReadInt32();
                        return value;
                    }
                case 'I':
                case 'i':
                    {
                        var value = binaryReader.ReadInt32();
                        return value;
                    }
                case 'L':
                case 'l':
                    {
                        var value = binaryReader.ReadInt64();
                        return value;
                    }
                case 'F':
                case 'f':
                    {
                        var value = binaryReader.ReadSingle();
                        return value;
                    }
                case 'D':
                case 'd':
                    {
                        var value = binaryReader.ReadDouble();
                        return (float)(FbxReader.FBXConversionPrecision * value);
                    }
            }

            return default(float);
        }

        private double BinaryGetDoubleValue(int index)
        {
            var binaryProperty = Values[index];
            Processor.FBXBinaryReader.BaseStream.Seek(binaryProperty.Position, SeekOrigin.Begin);
            binaryProperty.GetPropertyData(this, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
            return BinaryConvertDoubleValue(Processor.FBXBinaryReader, binaryProperty, propertyType);
        }

        public double BinaryConvertDoubleValue(BinaryReader binaryReader, FBXProperty binaryProperty, char propertyType)
        {
            switch (propertyType)
            {
                case 'R':
                case 'C':
                case 'c':
                    {
                        var value = binaryReader.ReadByte();
                        return value;
                    }
                case 'Y':
                case 'y':
                    {
                        var value = binaryReader.ReadInt16();
                        return value;
                    }
                case 'B':
                case 'b':
                    {
                        var value = binaryReader.ReadInt32();
                        return value;
                    }
                case 'I':
                case 'i':
                    {
                        var value = binaryReader.ReadInt32();
                        return value;
                    }
                case 'L':
                case 'l':
                    {
                        var value = binaryReader.ReadInt64();
                        return value;
                    }
                case 'F':
                case 'f':
                    {
                        var value = binaryReader.ReadSingle();
                        return value;
                    }
                case 'D':
                case 'd':
                    {
                        var value = binaryReader.ReadDouble();
                        return value;
                    }
            }

            return default(double);
        }

        public PropertyAccessorByte GetByteValues()
        {
            return new PropertyAccessorByte(this);
        }

        private PropertyAccessorShort GetShortValues()
        {
            return new PropertyAccessorShort(this);
        }

        public PropertyAccessorIEE754 GetIEE754Values()
        {
            return new PropertyAccessorIEE754(this);
        }

        public PropertyAccessorInt GetIntValues()
        {
            return new PropertyAccessorInt(this);
        }

        public PropertyAccessorLong GetLongValues(bool isTime = false)
        {
            return new PropertyAccessorLong(this, isTime);
        }

        public PropertyAccessorFloat GetFloatValues()
        {
            return new PropertyAccessorFloat(this);
        }

        private PropertyAccessorDouble GetDoubleValues()
        {
            return new PropertyAccessorDouble(this);
        }

        private bool BinaryGetBoolValue(int index)
        {
            return BinaryGetIntValue(index) == 1;
        }

        private Vector2 BinaryGetVector2Value(int index)
        {
            var x = BinaryGetFloatValue(index);
            var y = BinaryGetFloatValue(index + 1);
            return new Vector2(x, y);
        }

        private Vector3 BinaryGetVector3Value(int index)
        {
            var x = BinaryGetFloatValue(index);
            var y = BinaryGetFloatValue(index + 1);
            var z = BinaryGetFloatValue(index + 2);
            return new Vector3(x, y, z);
        }

        private Vector4 BinaryGetVector4Value(int index)
        {
            var x = BinaryGetFloatValue(index);
            var y = BinaryGetFloatValue(index + 1);
            var z = BinaryGetFloatValue(index + 2);
            var w = BinaryGetFloatValue(index + 3);
            return new Vector4(x, y, z, w);
        }

        private Color BinaryGetColorValue(int index)
        {
            var x = BinaryGetFloatValue(index);
            var y = BinaryGetFloatValue(index + 1);
            var z = BinaryGetFloatValue(index + 2);
            return new Color(x, y, z, 1f);
        }

        private Color BinaryGetColorAlphaValue(int index)
        {
            var x = BinaryGetFloatValue(index);
            var y = BinaryGetFloatValue(index + 1);
            var z = BinaryGetFloatValue(index + 2);
            var w = BinaryGetFloatValue(index + 3);
            return new Color(x, y, z, w);
        }

        private Matrix4x4 BinaryGetMatrixValue()
        {
            var matrix = Matrix4x4.identity;
            var values = GetFloatValues();
            for (var i = 0; i < values.Count; i++)
            {
                matrix[i] = values[i];
                if (i >= 16)
                {
                    break;
                }
            }
            return matrix;
        }

        private int BinaryGetPropertyArrayLength()
        {
            if (ArrayLength > 0)
            {
                return ArrayLength;
            }
            if (Values.Count == 0)
            {
                return 0;
            }
            var binaryProperty = Values[0];
            binaryProperty.GetPropertyData(this, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
            return arrayLength;
        }
        #endregion

        #region ASCII

        private string ASCIIGetProperty(int index, bool isNumber, bool hashOnly)
        {
            var property = Values[index];
            return Processor.ActiveASCIIReader.GetPropertyStringValue(property, isNumber, hashOnly);
        }

        public IEnumerator<byte> ASCIIGetPropertyEnumerator()
        {
            var property = Values[0];
            return Processor.ActiveASCIIReader.GetPropertyStringValueEnumerator(property);
        }

        private string ASCIIGetStringValue(int index, bool hashOnly)
        {
            var property = ASCIIGetProperty(index, false, hashOnly);
            return property;
        }

        public byte ASCIIGetByteValue(int index)
        {
            var property = ASCIIGetProperty(index, true, false);
            byte.TryParse(property, out var value);
            return value;
        }

        public short ASCIIGetShortValue(int index)
        {
            var property = ASCIIGetProperty(index, true, false);
            short.TryParse(property, out var value);
            return value;
        }

        public int ASCIIGetIntValue(int index)
        {
            var property = ASCIIGetProperty(index, true, false);
            int.TryParse(property, out var value);
            return value;
        }

        public long ASCIIGetLongValue(int index)
        {
            var property = ASCIIGetProperty(index, true, false);
            long.TryParse(property, out var value);
            return value;
        }

        public float ASCIIGetFloatValue(int index)
        {
            var property = ASCIIGetProperty(index, true, false);
            float.TryParse(property, NumberStyles.Any, CultureInfo.InvariantCulture, out var value);
            return value;
        }

        public double ASCIIGetDoubleValue(int index)
        {
            var property = ASCIIGetProperty(index, true, false);
            double.TryParse(property, NumberStyles.Any, CultureInfo.InvariantCulture, out var value);
            return value;
        }

        private bool ASCIIGetBoolValue(int index)
        {
            var property = ASCIIGetProperty(index, true, false);
            int.TryParse(property, out var value);
            return value != 0;
        }

        private Vector2 ASCIIGetVector2Value(int index)
        {
            var x = ASCIIGetFloatValue(index + 0);
            var y = ASCIIGetFloatValue(index + 1);
            return new Vector2(x, y);
        }

        private Vector3 ASCIIGetVector3Value(int index)
        {
            var x = ASCIIGetFloatValue(index + 0);
            var y = ASCIIGetFloatValue(index + 1);
            var z = ASCIIGetFloatValue(index + 2);
            return new Vector3(x, y, z);
        }

        private Vector4 ASCIIGetVector4Value(int index)
        {
            var x = ASCIIGetFloatValue(index + 0);
            var y = ASCIIGetFloatValue(index + 1);
            var z = ASCIIGetFloatValue(index + 2);
            var w = ASCIIGetFloatValue(index + 3);
            return new Vector4(x, y, z, w);
        }

        private Color ASCIIGetColorValue(int index)
        {
            var x = ASCIIGetFloatValue(index + 0);
            var y = ASCIIGetFloatValue(index + 1);
            var z = ASCIIGetFloatValue(index + 2);
            return new Color(x, y, z, 1f);
        }

        private Color ASCIIGetColorAlphaValue(int index)
        {
            var x = ASCIIGetFloatValue(index + 0);
            var y = ASCIIGetFloatValue(index + 1);
            var z = ASCIIGetFloatValue(index + 2);
            var w = ASCIIGetFloatValue(index + 3);
            return new Color(x, y, z, w);
        }

        private Matrix4x4 ASCIIGetMatrixValue()
        {
            var matrix = new Matrix4x4();
            for (var i = 0; i < 16; i++)
            {
                matrix[i] = ASCIIGetFloatValue(i);
            }
            return matrix;
        }
        #endregion

        public FBXProperty this[int i] => Values[i];
    }
}
