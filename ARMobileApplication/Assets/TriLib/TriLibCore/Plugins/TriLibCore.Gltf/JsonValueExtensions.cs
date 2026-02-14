using System;
using System.Globalization;
using TriLibCore.Utils;

namespace TriLibCore.Gltf
{
    public static partial class JsonValueExtensions
    {
        public static bool TryGetChildValueAsString(this JsonParser.JsonValue value, long fieldName, out string outValue, TemporaryString temporaryString, string defaultValue = null)
        {
            var property = value.GetChildWithKey(fieldName);
            if (!property.Valid)
            {
                outValue = defaultValue;
                return false;
            }
            outValue = GetValueAsString(property, temporaryString, defaultValue);
            return true;
        }

        public static string GetChildValueAsString(this JsonParser.JsonValue value, long fieldName, TemporaryString temporaryString, string defaultValue = null)
        {
            var result = TryGetChildValueAsString(value, fieldName, out var outValue, temporaryString, defaultValue);
            return outValue;
        }

        public static bool HasValue(this JsonParser.JsonValue value, long fieldName)
        {
            var property = value.GetChildWithKey(fieldName);
            return property.Valid;
        }

        public static bool TryGetValueAsString(this JsonParser.JsonValue value, out string outValue, TemporaryString temporaryString, string defaultValue = default)
        {
            outValue = value.Valid ? value.ToString() : defaultValue;
            return value.Valid;
        }

        public static string GetValueAsString(this JsonParser.JsonValue value, TemporaryString temporaryString, string defaultValue = default)
        {
            var result = TryGetValueAsString(value, out var outValue, temporaryString, defaultValue);
            return outValue;
        }

        public static bool TryGetValueAsFloat(this JsonParser.JsonValue value, out float outValue, TemporaryString temporaryString, float defaultValue = default)
        {
            var result = float.TryParse(temporaryString.GetString(value), NumberStyles.Float, CultureInfo.InvariantCulture, out outValue);
            return result;
        }

        public static bool TryGetValueAsInt(this JsonParser.JsonValue value, out int outValue, TemporaryString temporaryString, int defaultValue = default)
        {
            var result = int.TryParse(temporaryString.GetString(value), out outValue);
            return result;
        }

        public static float GetValueAsFloat(this JsonParser.JsonValue value, TemporaryString temporaryString, float defaultValue = default)
        {
            if (TryGetValueAsFloat(value, out var outValue, temporaryString, defaultValue))
            {
                return outValue;
            }
            return defaultValue;
        }

        public static int GetValueAsInt(this JsonParser.JsonValue value, TemporaryString temporaryString, int defaultValue = default)
        {
            if (TryGetValueAsInt(value, out var outValue, temporaryString, defaultValue))
            {
                return outValue;
            }
            return defaultValue;
        }

        public static bool TryGetChildValueAsFloat(this JsonParser.JsonValue value, long fieldName, out float outValue, TemporaryString temporaryString, float defaultValue = default)
        {
            var property = value.GetChildWithKey(fieldName);
            if (!property.Valid)
            {
                outValue = defaultValue;
                return false;
            }
            return TryGetValueAsFloat(property, out outValue, temporaryString, defaultValue);
        }

        public static float GetChildValueAsFloat(this JsonParser.JsonValue value, long fieldName, TemporaryString temporaryString, float defaultValue = default)
        {
            if (TryGetChildValueAsFloat(value, fieldName, out var outValue, temporaryString, defaultValue))
            {
                return outValue;
            }
            return defaultValue;
        }

        public static bool TryGetChildValueAsInt(this JsonParser.JsonValue value, long fieldName, out int outValue, TemporaryString temporaryString, int defaultValue = default)
        {
            var property = value.GetChildWithKey(fieldName);
            if (!property.Valid)
            {
                outValue = defaultValue;
                return false;
            }
            return TryGetValueAsInt(property, out outValue, temporaryString, defaultValue);
        }

        public static int GetChildValueAsInt(this JsonParser.JsonValue value, long fieldName, TemporaryString temporaryString, int defaultValue = default)
        {
            if (TryGetChildValueAsInt(value, fieldName, out var outValue, temporaryString, defaultValue))
            {
                return outValue;
            }
            return defaultValue;
        }

        public static JsonParser.JsonValue GetArrayValueAtIndex(this JsonParser.JsonValue value, int index)
        {
            if (value.Type != JsonParser.JsonValueType.Array)
            {
                throw new Exception("Value is not an array");
            }
            return value.GetChildAtIndex(index);
        }

        public static bool TryGetChildWithKey(this JsonParser.JsonValue value, long key, out JsonParser.JsonValue outValue)
        {
            outValue = value.GetChildWithKey(key);
            return outValue.Valid;
        }

        public static bool StartsWith(this JsonParser.JsonValue value, string prefix, StringComparison stringComparison = StringComparison.InvariantCulture)
        {
            using (var charEnumerator = value.GetCharEnumerator())
            {
                var index = 0;
                while (index < prefix.Length && charEnumerator.MoveNext())
                {
                    if (prefix[index] != charEnumerator.Current)
                    {
                        return false;
                    }
                    index++;
                }
                return true;
            }
        }
    }
}