using System;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXMatrixBuffer
    {
        private const int MatrixCount = 32;
        
        private readonly float[][] _matrixPool = new float[MatrixCount][];
        private int _matrixIndex;

        public FBXMatrixBuffer()
        {
            for (var i = 0; i < MatrixCount; i++)
            {
                _matrixPool[i] = new float[16];
            }
        }

        public float[] Identity
        {
            get
            {
                var matrix = _matrixPool[_matrixIndex++ % MatrixCount];
                matrix[0] = 1.0f;
                matrix[1] = 0.0f;
                matrix[2] = 0.0f;
                matrix[3] = 0.0f;

                matrix[4] = 0.0f;
                matrix[5] = 1.0f;
                matrix[6] = 0.0f;
                matrix[7] = 0.0f;

                matrix[8] = 0.0f;
                matrix[9] = 0.0f;
                matrix[10] = 1.0f;
                matrix[11] = 0.0f;

                matrix[12] = 0.0f;
                matrix[13] = 0.0f;
                matrix[14] = 0.0f;
                matrix[15] = 1.0f;
                return matrix;
            }
        }

        public float[] Scale(Vector3 vector)
        {
            var matrix = Identity;
            matrix[0] = vector.x;
            matrix[1] = 0.0f;
            matrix[2] = 0.0f;
            matrix[3] = 0.0f;

            matrix[4] = 0.0f;
            matrix[5] = vector.y;
            matrix[6] = 0.0f;
            matrix[7] = 0.0f;

            matrix[8] = 0.0f;
            matrix[9] = 0.0f;
            matrix[10] = vector.z;
            matrix[11] = 0.0f;

            matrix[12] = 0.0f;
            matrix[13] = 0.0f;
            matrix[14] = 0.0f;
            matrix[15] = 1.0f;
            return matrix;
        }

        public float[] Translate(Vector3 vector)
        {
            var matrix = Identity;
            matrix[0] = 1.0f;
            matrix[1] = 0.0f;
            matrix[2] = 0.0f;
            matrix[3] = 0.0f;

            matrix[4] = 0.0f;
            matrix[5] = 1.0f;
            matrix[6] = 0.0f;
            matrix[7] = 0.0f;

            matrix[8] = 0.0f;
            matrix[9] = 0.0f;
            matrix[10] = 1.0f;
            matrix[11] = 0.0f;

            matrix[12] = vector.x;
            matrix[13] = vector.y;
            matrix[14] = vector.z;
            matrix[15] = 1.0f;
            return matrix;
        }

        public float[] Rotate(Quaternion q)
        {
            var res = Identity;
            if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w))
            {
                return res;
            }

            // Precalculate coordinate products
            var x = q.x * 2.0f;
            var y = q.y * 2.0f;
            var z = q.z * 2.0f;
            var xx = q.x * x;
            var yy = q.y * y;
            var zz = q.z * z;
            var xy = q.x * y;
            var xz = q.x * z;
            var yz = q.y * z;
            var wx = q.w * x;
            var wy = q.w * y;
            var wz = q.w * z;

            // Calculate 3x3 matrix from orthonormal basis
            res[0] = 1.0f - (yy + zz);
            res[1] = xy + wz;
            res[2] = xz - wy;
            res[3] = 0.0f;

            res[4] = xy - wz;
            res[5] = 1.0f - (xx + zz);
            res[6] = yz + wx;
            res[7] = 0.0f;

            res[8] = xz + wy;
            res[9] = yz - wx;
            res[10] = 1.0f - (xx + yy);
            res[11] = 0.0f;

            res[12] = 0.0f;
            res[13] = 0.0f;
            res[14] = 0.0f;
            res[15] = 1.0f;
            return res;
        }

        public float[] Invert(float[] m)
        {
            var inv = Identity;
            var invOut = Identity;

            float det;
            int i;

            inv[0] = m[5] * m[10] * m[15] -
                     m[5] * m[11] * m[14] -
                     m[9] * m[6] * m[15] +
                     m[9] * m[7] * m[14] +
                     m[13] * m[6] * m[11] -
                     m[13] * m[7] * m[10];

            inv[4] = -m[4] * m[10] * m[15] +
                      m[4] * m[11] * m[14] +
                      m[8] * m[6] * m[15] -
                      m[8] * m[7] * m[14] -
                      m[12] * m[6] * m[11] +
                      m[12] * m[7] * m[10];

            inv[8] = m[4] * m[9] * m[15] -
                     m[4] * m[11] * m[13] -
                     m[8] * m[5] * m[15] +
                     m[8] * m[7] * m[13] +
                     m[12] * m[5] * m[11] -
                     m[12] * m[7] * m[9];

            inv[12] = -m[4] * m[9] * m[14] +
                       m[4] * m[10] * m[13] +
                       m[8] * m[5] * m[14] -
                       m[8] * m[6] * m[13] -
                       m[12] * m[5] * m[10] +
                       m[12] * m[6] * m[9];

            inv[1] = -m[1] * m[10] * m[15] +
                      m[1] * m[11] * m[14] +
                      m[9] * m[2] * m[15] -
                      m[9] * m[3] * m[14] -
                      m[13] * m[2] * m[11] +
                      m[13] * m[3] * m[10];

            inv[5] = m[0] * m[10] * m[15] -
                     m[0] * m[11] * m[14] -
                     m[8] * m[2] * m[15] +
                     m[8] * m[3] * m[14] +
                     m[12] * m[2] * m[11] -
                     m[12] * m[3] * m[10];

            inv[9] = -m[0] * m[9] * m[15] +
                      m[0] * m[11] * m[13] +
                      m[8] * m[1] * m[15] -
                      m[8] * m[3] * m[13] -
                      m[12] * m[1] * m[11] +
                      m[12] * m[3] * m[9];

            inv[13] = m[0] * m[9] * m[14] -
                      m[0] * m[10] * m[13] -
                      m[8] * m[1] * m[14] +
                      m[8] * m[2] * m[13] +
                      m[12] * m[1] * m[10] -
                      m[12] * m[2] * m[9];

            inv[2] = m[1] * m[6] * m[15] -
                     m[1] * m[7] * m[14] -
                     m[5] * m[2] * m[15] +
                     m[5] * m[3] * m[14] +
                     m[13] * m[2] * m[7] -
                     m[13] * m[3] * m[6];

            inv[6] = -m[0] * m[6] * m[15] +
                      m[0] * m[7] * m[14] +
                      m[4] * m[2] * m[15] -
                      m[4] * m[3] * m[14] -
                      m[12] * m[2] * m[7] +
                      m[12] * m[3] * m[6];

            inv[10] = m[0] * m[5] * m[15] -
                      m[0] * m[7] * m[13] -
                      m[4] * m[1] * m[15] +
                      m[4] * m[3] * m[13] +
                      m[12] * m[1] * m[7] -
                      m[12] * m[3] * m[5];

            inv[14] = -m[0] * m[5] * m[14] +
                       m[0] * m[6] * m[13] +
                       m[4] * m[1] * m[14] -
                       m[4] * m[2] * m[13] -
                       m[12] * m[1] * m[6] +
                       m[12] * m[2] * m[5];

            inv[3] = -m[1] * m[6] * m[11] +
                      m[1] * m[7] * m[10] +
                      m[5] * m[2] * m[11] -
                      m[5] * m[3] * m[10] -
                      m[9] * m[2] * m[7] +
                      m[9] * m[3] * m[6];

            inv[7] = m[0] * m[6] * m[11] -
                     m[0] * m[7] * m[10] -
                     m[4] * m[2] * m[11] +
                     m[4] * m[3] * m[10] +
                     m[8] * m[2] * m[7] -
                     m[8] * m[3] * m[6];

            inv[11] = -m[0] * m[5] * m[11] +
                       m[0] * m[7] * m[9] +
                       m[4] * m[1] * m[11] -
                       m[4] * m[3] * m[9] -
                       m[8] * m[1] * m[7] +
                       m[8] * m[3] * m[5];

            inv[15] = m[0] * m[5] * m[10] -
                      m[0] * m[6] * m[9] -
                      m[4] * m[1] * m[10] +
                      m[4] * m[2] * m[9] +
                      m[8] * m[1] * m[6] -
                      m[8] * m[2] * m[5];

            det = m[0] * inv[0] + m[1] * inv[4] + m[2] * inv[8] + m[3] * inv[12];

            if (det == 0)
            {
                throw new Exception("Could not invert matrix");
            }

            det = 1.0f / det;

            for (i = 0; i < 16; i++)
            {
                invOut[i] = inv[i] * det;
            }

            return invOut;
        }

        public float this[int i] {
            get => _matrixPool[_matrixIndex][i];
            set => _matrixPool[_matrixIndex][i] = value;
        }
    }
}