#pragma warning disable 168
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TriLibCore.Extensions
{
    /// <summary>Represents a series of Stream extension methods.</summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Reads all Stream Bytes into a Byte Array when the Stream has unknown length.
        /// </summary>
        /// <param name="input">The input Stream.</param>
        /// <returns>The Stream Data Bytes.</returns>
        public static byte[] ReadBytesUnknownLength(this Stream input)
        {
            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream()) 
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }


        /// <summary>
        /// Reads all Stream Bytes into a Byte Array.
        /// </summary>
        /// <param name="input">The input Stream.</param>
        /// <param name="bufferCallback">The callback used to retrieve the byte buffer where the Stream data will be write to.</param>
        /// <param name="customData">A custom parameter to be sent to the buffer callback.</param>
        /// <returns>The Stream Data Bytes.</returns>
        public static byte[] ReadBytes(this Stream input, Func<int, object, byte[]> bufferCallback, object customData = null)
        {
            if (input is MemoryStream memoryStream)
            {
                try
                {
                    return memoryStream.GetBuffer();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("You are passing a MemoryStream without GetBuffer() access. For better memory usage, please create your MemoryStream with 'publiclyVisible = true'.");
                }
            }
            if (input.Length == 0)
            {
                Debug.LogWarning("Input Stream has invalid or no Length.");
                return ReadBytes(input);
            }
            var buffer = bufferCallback((int)input.Length, customData);
            input.Read(buffer, 0, (int)input.Length);
            return buffer;
        }


        /// <summary>
        /// Reads all Stream Bytes into a Byte Array.
        /// </summary>
        /// <param name="input">The input Stream.</param>
        /// <returns>The Stream Data Bytes.</returns>
        public static byte[] ReadBytes(this Stream input)
        {
            if (input is MemoryStream memoryStream)
            {
                try
                {
                    return memoryStream.GetBuffer();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("You are passing a MemoryStream without GetBuffer() access. For better memory usage, please create your MemoryStream with 'publiclyVisible = true'.");
                }
            }
            if (input.Length == 0)
            {
                Debug.LogWarning("Input Stream has invalid or no Length.");
                return ReadBytesUnknownLength(input);
            }
            var buffer = new byte[(int)input.Length];
            input.Read(buffer, 0, (int)input.Length);
            return buffer;
        }


        /// <summary>Tries to match the given RegEx patterns on the Stream contents.</summary>
        /// <param name="stream">The Stream to perform the search.</param>
        /// <param name="patterns">The list of RegEx patterns to search for.</param>
        /// <returns>
        /// <c>true</c>, if all patterns match. Otherwise, <c>false</c>.</returns>
        public static bool MatchRegex(this Stream stream, params string[] patterns)
        {
            var matchedAll = true;
            var streamReader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
            for (var i = 0; i < patterns.Length; i++)
            {
                var pattern = patterns[i];
                var matched = false;
                var regex = new Regex(pattern);
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    if (line != null && regex.Match(line).Success)
                    {
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                {
                    matchedAll = false;
                    break;
                }
            }
            stream.Seek(0, SeekOrigin.Begin);
            return matchedAll;
        }

        /// <summary>
        /// Tries to dispose the given Stream.
        /// </summary>
        /// <param name="stream">The Stream to dispose.</param>
        public static void TryToDispose(this Stream stream)
        {
            try
            {
                stream.Dispose();
            }
            catch
            {
                //ignored
            }
        }
    }
}
