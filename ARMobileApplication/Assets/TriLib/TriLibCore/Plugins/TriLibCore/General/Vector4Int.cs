using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.General
{
    /// <summary>
    /// Represents a four-dimensional vector of integer components, and provides
    /// implicit and explicit conversions to and from Unity's <see cref="Vector2"/>, 
    /// <see cref="Vector3"/>, <see cref="Vector4"/>, and <see cref="Color"/>.
    /// </summary>
    public struct Vector4Int : IList<int>
    {
        /// <summary>
        /// The X component of the vector.
        /// </summary>
        public int x;

        /// <summary>
        /// The Y component of the vector.
        /// </summary>
        public int y;

        /// <summary>
        /// The Z component of the vector.
        /// </summary>
        public int z;

        /// <summary>
        /// The W component of the vector.
        /// </summary>
        public int w;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector4Int"/> struct
        /// with the specified <paramref name="x"/>, <paramref name="y"/>, 
        /// <paramref name="z"/>, and <paramref name="w"/> components.
        /// </summary>
        /// <param name="x">The X component value.</param>
        /// <param name="y">The Y component value.</param>
        /// <param name="z">The Z component value.</param>
        /// <param name="w">The W component value.</param>
        public Vector4Int(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        /// <inheritdoc />
        public IEnumerator<int> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(int item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Contains(int item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void CopyTo(int[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Remove(int item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public int Count => 4;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public int IndexOf(int item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Insert(int index, int item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets the component at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">A zero-based index of the component to get or set.</param>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when <paramref name="index"/> is not in [0..3].
        /// </exception>
        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return x;
                    case 1:
                        return y;
                    case 2:
                        return z;
                    case 3:
                        return w;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    case 3:
                        w = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Explicitly converts a <see cref="Vector2"/> to a <see cref="Vector4Int"/>,
        /// setting the <c>X</c> and <c>Y</c> components and defaulting <c>Z</c> and <c>W</c> to zero.
        /// </summary>
        /// <param name="b">The <see cref="Vector2"/> to convert.</param>
        public static explicit operator Vector4Int(Vector2 b)
            => new Vector4Int((int)b.x, (int)b.y, 0, 0);

        /// <summary>
        /// Explicitly converts a <see cref="Vector3"/> to a <see cref="Vector4Int"/>,
        /// setting the <c>X</c>, <c>Y</c>, and <c>Z</c> components and defaulting <c>W</c> to zero.
        /// </summary>
        /// <param name="b">The <see cref="Vector3"/> to convert.</param>
        public static explicit operator Vector4Int(Vector3 b)
            => new Vector4Int((int)b.x, (int)b.y, (int)b.z, 0);

        /// <summary>
        /// Explicitly converts a <see cref="Vector4"/> to a <see cref="Vector4Int"/>,
        /// setting all four integer components.
        /// </summary>
        /// <param name="b">The <see cref="Vector4"/> to convert.</param>
        public static explicit operator Vector4Int(Vector4 b)
            => new Vector4Int((int)b.x, (int)b.y, (int)b.z, (int)b.w);

        /// <summary>
        /// Explicitly converts a <see cref="Color"/> to a <see cref="Vector4Int"/>,
        /// interpreting each color channel as an integer component.
        /// </summary>
        /// <param name="b">The <see cref="Color"/> to convert.</param>
        public static explicit operator Vector4Int(Color b)
            => new Vector4Int((int)b.r, (int)b.g, (int)b.b, (int)b.a);

        /// <summary>
        /// Implicitly converts a <see cref="Vector4Int"/> to a <see cref="Vector2"/>,
        /// discarding the <c>Z</c> and <c>W</c> components.
        /// </summary>
        /// <param name="b">The <see cref="Vector4Int"/> to convert.</param>
        public static implicit operator Vector2(Vector4Int b)
            => new Vector2(b.x, b.y);

        /// <summary>
        /// Implicitly converts a <see cref="Vector4Int"/> to a <see cref="Vector3"/>,
        /// discarding the <c>W</c> component.
        /// </summary>
        /// <param name="b">The <see cref="Vector4Int"/> to convert.</param>
        public static implicit operator Vector3(Vector4Int b)
            => new Vector3(b.x, b.y, b.z);

        /// <summary>
        /// Implicitly converts a <see cref="Vector4Int"/> to a <see cref="Vector4"/>.
        /// </summary>
        /// <param name="b">The <see cref="Vector4Int"/> to convert.</param>
        public static implicit operator Vector4(Vector4Int b)
            => new Vector4(b.x, b.y, b.z, b.w);

        /// <summary>
        /// Implicitly converts a <see cref="Vector4Int"/> to a <see cref="Color"/>.
        /// Each integer component <c>x, y, z, w</c> is cast to a float for the respective color channel.
        /// </summary>
        /// <param name="b">The <see cref="Vector4Int"/> to convert.</param>
        public static implicit operator Color(Vector4Int b)
            => new Color(b.x, b.y, b.z, b.w);
    }
}
