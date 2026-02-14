using System.Collections.Generic;

namespace TriLibCore.Ply
{
    public class PlyElement
    {
        public readonly Dictionary<long, PlyProperty> Properties = new Dictionary<long, PlyProperty>();
        public int Count;
        public List<List<PlyValue>> Data;

        public PlyProperty GetProperty(long propertyName)
        {
            if (Properties.TryGetValue(propertyName, out var property))
            {
                return property;
            }
            return null;
        }

        public int GetListIndex(PlyListProperty property, int elementIndex)
        {
            if (elementIndex < Data.Count)
            {
                return PlyValue.GetIntValue(Data[elementIndex][property.Offset], PlyPropertyType.Int);
            }
            return default;
        }

        public int GetPropertyIntValue(PlyProperty property, int elementIndex)
        {
            if (elementIndex < Data.Count)
            {
                return PlyValue.GetIntValue(Data[elementIndex][property.Offset], property);
            }
            return default;
        }

        public float GetPropertyFloatValue(PlyProperty property, int elementIndex)
        {
            if (elementIndex < Data.Count)
            {
                return PlyValue.GetFloatValue(Data[elementIndex][property.Offset], property);
            }
            return default;
        }

        public int GetPropertyIntValue(long propertyName, int elementIndex)
        {
            if (Properties.TryGetValue(propertyName, out var property) && elementIndex < Data.Count)
            {
                return PlyValue.GetIntValue(Data[elementIndex][property.Offset], property);
            }
            return default;
        }

        public float GetPropertyFloatValue(long propertyName, int elementIndex)
        {
            if (Properties.TryGetValue(propertyName, out var property) && elementIndex < Data.Count)
            {
                return PlyValue.GetFloatValue(Data[elementIndex][property.Offset], property);
            }
            return default;
        }

        public PlyValue GetPropertyValue(long propertyName, int elementIndex)
        {
            if (Properties.TryGetValue(propertyName, out var property) && elementIndex < Data.Count)
            {
                return Data[elementIndex][property.Offset];
            }
            return default;
        }
    }
}