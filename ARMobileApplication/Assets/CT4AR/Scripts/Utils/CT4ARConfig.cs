using UnityEngine;

namespace CT4AR.Utils
{
    /// <summary>
    /// ScriptableObject holding app-wide configuration (server URL, cache settings, etc.).
    /// Create via Assets > Create > CT4AR > App Config.
    /// </summary>
    [CreateAssetMenu(fileName = "CT4ARConfig", menuName = "CT4AR/App Config")]
    public class CT4ARConfig : ScriptableObject
    {
        [Header("Server")]
        public string apiBaseUrl = "https://api.ar4ct.com";

        [Header("Cache")]
        public bool enableModelCache = true;
        public int maxCachedModels = 20;

        [Header("AR")]
        public float defaultModelScale = 0.01f;
    }
}
