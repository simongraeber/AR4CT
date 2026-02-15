using System;
using UnityEngine;

namespace CT4AR.Networking
{
    /// <summary>
    /// JSON-serializable data returned by GET /scans/{scan_id}/bundle.
    /// The point is already transformed into FBX model space (metres, centred).
    /// </summary>
    [Serializable]
    public class ScanPointData
    {
        public string scan_id;
        public PointInfo point;
    }

    [Serializable]
    public class PointInfo
    {
        public float x;
        public float y;
        public float z;
        public string label;
        public string set_at;

        public Vector3 ToVector3() => new Vector3(x, z, y);
    }
}
