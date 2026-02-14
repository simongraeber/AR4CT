using System;

namespace TriLibCore.Extensions
{
    /// <summary>
    /// Represents a series of String extension methods.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Splits a string without causing memory allocation.
        /// </summary>
        /// <param name="value">String to split.</param>
        /// <param name="separator">Separator character.</param>
        /// <param name="buffer">The buffer to store the Strings.</param>
        /// <returns>The number of sub-strings.</returns>
        public static int SplitNoAlloc(this string value, char separator, string[] buffer)
        {
            if (value == null)
            {
                return 0;
            }
            const int indexNotFound = -1;
            var bufferLength = buffer.Length;
            var startIndex = 0;
            int index;
            var nextBufferIndex = 0;
            while ((index = value.IndexOf(separator, startIndex)) != indexNotFound)
            {
                if (++nextBufferIndex == bufferLength)
                {
                    buffer[nextBufferIndex - 1] = value.Substring(startIndex);
                    break;
                }
                buffer[nextBufferIndex - 1] = value.Substring(startIndex, index - startIndex);
                startIndex = index + 1;
            }
            if (nextBufferIndex < bufferLength && value.Length >= startIndex)
            {
                buffer[nextBufferIndex++] = string.Intern(value.Substring(startIndex));
            }
            return nextBufferIndex;
        }
    }
}
