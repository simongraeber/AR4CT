using UnityEngine;
using UnityEngine.Events;

namespace CT4AR.Networking
{
    /// <summary>
    /// Handles universal/deep links (e.g. https://ar4ct.com/app/demo).
    /// Attach to a GameObject in your scene. Wire up the Unity Events in the Inspector
    /// to show/hide UI elements depending on how the app was opened.
    /// </summary>
    public class DeepLinkHandler : MonoBehaviour
    {
        [Header("GameObjects")]
        [Tooltip("GameObject to activate when the app is opened via a deep link.")]
        public GameObject deepLinkTarget;

        [Tooltip("GameObject to activate when the app is opened normally (no deep link).")]
        public GameObject normalLaunchTarget;

        [Header("Events")]
        [Tooltip("Fired after deepLinkTarget is activated. Parameter = path segment (e.g. \"demo\").")]
        public UnityEvent<string> onDeepLinkActivated;

        [Tooltip("Fired after normalLaunchTarget is activated.")]
        public UnityEvent onNormalLaunch;

        /// <summary>The extracted path segment, or null if not launched via link.</summary>
        public string DeepLinkPath { get; private set; }

        /// <summary>True if the app was opened through a universal link.</summary>
        public bool WasOpenedViaLink => !string.IsNullOrEmpty(DeepLinkPath);

        void Awake()
        {
            // Subscribe to deep links that arrive while the app is already running
            Application.deepLinkActivated += OnDeepLinkActivated;

            // Check if the app was cold-started with a deep link
            string initialUrl = GetLaunchUrl();
            if (!string.IsNullOrEmpty(initialUrl) && IsValidDeepLink(initialUrl))
            {
                OnDeepLinkActivated(initialUrl);
            }
            else
            {
                if (!string.IsNullOrEmpty(initialUrl))
                    Debug.Log("[CT4AR] Ignoring non-deep-link launch URL: " + initialUrl);
                Debug.Log("[CT4AR] App opened normally (no deep link).");
                if (normalLaunchTarget != null) normalLaunchTarget.SetActive(true);
                onNormalLaunch?.Invoke();
            }
        }

        void OnDestroy()
        {
            Application.deepLinkActivated -= OnDeepLinkActivated;
        }

        private void OnDeepLinkActivated(string url)
        {
            Debug.Log("[CT4AR] Deep link received: " + url);
            DeepLinkPath = ExtractPathSegment(url);
            Debug.Log("[CT4AR] Extracted path: " + DeepLinkPath);
            if (deepLinkTarget != null) deepLinkTarget.SetActive(true);
            onDeepLinkActivated?.Invoke(DeepLinkPath);
        }

        /// <summary>
        /// Returns true if the URL looks like one of our deep links.
        /// </summary>
        private bool IsValidDeepLink(string url)
        {
            return !string.IsNullOrEmpty(url) && url.Contains("/app/");
        }

        /// <summary>
        /// Extracts the last meaningful path segment after "/app/".
        /// e.g. "https://ar4ct.com/app/demo" → "demo"
        ///      "https://ar4ct.com/app/patient/123" → "patient/123"
        /// </summary>
        private string ExtractPathSegment(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            const string marker = "/app/";
            int index = url.IndexOf(marker);
            if (index >= 0)
            {
                string segment = url.Substring(index + marker.Length).TrimEnd('/');
                return string.IsNullOrEmpty(segment) ? null : segment;
            }

            // Fallback: return everything after the last '/'
            int lastSlash = url.LastIndexOf('/');
            if (lastSlash >= 0 && lastSlash < url.Length - 1)
                return url.Substring(lastSlash + 1);

            return null;
        }

        /// <summary>
        /// Gets the URL that launched the app (cold start).
        /// Uses Unity's built-in absoluteURL on all platforms,
        /// with an Android intent fallback.
        /// </summary>
        private string GetLaunchUrl()
        {
            // Unity 2021.2+ exposes the launch URL on all platforms
            if (!string.IsNullOrEmpty(Application.absoluteURL))
                return Application.absoluteURL;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
                {
                    return intent.Call<string>("getDataString");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CT4AR] Failed to read Android intent: " + e.Message);
            }
#endif
            return null;
        }
    }
}
