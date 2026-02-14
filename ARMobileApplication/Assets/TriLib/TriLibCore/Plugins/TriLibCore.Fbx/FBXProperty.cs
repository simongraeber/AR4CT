using System;
using System.IO;

namespace TriLibCore.Fbx
{
    public class FBXProperty 
    {
        public long Position;
        public int StringCharLength;
        public static int GetDataSize(char propertyType, int arrayLength, out int subDataSize)
        {
            subDataSize = GetSubDataSize(propertyType);
            return subDataSize * arrayLength;
        }

        public void GetPropertyData(FBXProperties properties, out char propertyType, out int compressedLength, out bool encoded, out int arrayLength, out int innerDataLength)
        {
            if (properties.IsASCII)
            {
                encoded = false;
                arrayLength = 0;
                compressedLength = 0;
                innerDataLength = 0;
                propertyType = 'S';
            }
            else
            {
                properties.Processor.ActiveBinaryReader.BaseStream.Seek(Position, SeekOrigin.Begin);
                propertyType = properties.Processor.ActiveBinaryReader.ReadChar();
                switch (propertyType)
                {
                    case 'S':
                    case 'R':
                        {
                            arrayLength = properties.Processor.ActiveBinaryReader.ReadInt32();
                            encoded = false;
                            compressedLength = 0;
                            innerDataLength = 0;
                            break;
                        }
                    case 'b':
                    case 'c':
                    case 'y':
                    case 'i':
                    case 'f':
                    case 'd':
                    case 'l':
                        {
                            arrayLength = properties.Processor.ActiveBinaryReader.ReadInt32();
                            encoded = properties.Processor.ActiveBinaryReader.ReadInt32() == 1;
                            compressedLength = properties.Processor.ActiveBinaryReader.ReadInt32();
                            innerDataLength = 0;
                            break;
                        }
                    default:
                        {
                            switch (propertyType)
                            {
                                case 'B':
                                case 'C':
                                    innerDataLength = sizeof(byte);
                                    break;
                                case 'Y':
                                    innerDataLength = sizeof(short);
                                    break;
                                case 'I':
                                case 'F':
                                    innerDataLength = sizeof(int);
                                    break;
                                case 'D':
                                case 'L':
                                    innerDataLength = sizeof(long);
                                    break;
                                default:
                                    throw new Exception($"Unknown data type: {propertyType}");
                            }

                            arrayLength = 1;
                            encoded = false;
                            compressedLength = 0;
                            break;
                        }
                }
            }
        }
        private static int GetSubDataSize(char propertyType)
        {
            int dataSize;
            switch (propertyType)
            {
                case 'B':
                case 'b':
                case 'C':
                case 'c':
                case 'R':
                case 'S':
                    dataSize = sizeof(byte);
                    break;
                case 'Y':
                case 'y':
                    dataSize = sizeof(short);
                    break;
                case 'I':
                case 'i':
                    dataSize = sizeof(int);
                    break;
                case 'F':
                case 'f':
                    dataSize = sizeof(float);
                    break;
                case 'D':
                case 'd':
                    dataSize = sizeof(double);
                    break;
                case 'L':
                case 'l':
                    dataSize = sizeof(long);
                    break;
                default:
                    return -1;
            }
            return dataSize;
        }
    }
}
