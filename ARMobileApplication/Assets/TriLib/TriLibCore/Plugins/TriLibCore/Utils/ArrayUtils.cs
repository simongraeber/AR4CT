using System;
using System.Collections.Generic;
using System.IO;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides utility methods for manipulating arrays and byte-based streams.
    /// </summary>
    public static class ArrayUtils
    {
        /// <summary>
        /// Copies the contents of the specified list of bytes into the specified <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="list">The source list of bytes to copy from.</param>
        /// <param name="memoryStream">
        /// A reference to the <see cref="MemoryStream"/> where the bytes will be copied. 
        /// If the underlying buffer of <paramref name="memoryStream"/> is empty, a new buffer of the appropriate size is created.
        /// </param>
        public static void ToMemoryStream(this IList<byte> list, ref MemoryStream memoryStream)
        {
            var buffer = memoryStream.GetBuffer();
            if (buffer.Length == 0)
            {
                buffer = new byte[list.Count];
                memoryStream = new MemoryStream(buffer);
            }
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = list[i];
            }
        }

        /// <summary>
        /// Adds an item to the end of the specified array, resizing the array to accommodate the new element.
        /// </summary>
        /// <typeparam name="T">The type of elements contained within the array.</typeparam>
        /// <param name="arr">A reference to the array to which the item will be added.</param>
        /// <param name="item">The item to add to the array.</param>
        public static void Add<T>(ref T[] arr, T item)
        {
            var newArr = new T[arr.Length + 1];
            int i;
            for (i = 0; i < arr.Length; i++)
            {
                newArr[i] = arr[i];
            }
            newArr[i] = item;
            arr = newArr;
        }

        /// <summary>
        /// Removes the first occurrence of a specified item from the specified array, resizing the array if the item is found.
        /// </summary>
        /// <typeparam name="T">The type of elements contained within the array.</typeparam>
        /// <param name="arr">A reference to the array from which the item will be removed.</param>
        /// <param name="item">The item to remove from the array.</param>
        public static void Remove<T>(ref T[] arr, T item)
        {
            var index = Array.IndexOf(arr, item);
            if (index < 0)
            {
                return;
            }
            for (var a = index; a < arr.Length - 1; a++)
            {
                arr[a] = arr[a + 1];
            }
            Array.Resize(ref arr, arr.Length - 1);
        }

        /// <summary>
        /// Determines whether the specified array contains at least one element of a given type.
        /// </summary>
        /// <typeparam name="T">The type to look for within the array.</typeparam>
        /// <param name="array">The array to search.</param>
        /// <returns>
        /// <see langword="true"/> if the array contains at least one element of the specified type; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool ContainsType<T>(Array array)
        {
            if (array != null)
            {
                foreach (var item in array)
                {
                    if (item is T)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
