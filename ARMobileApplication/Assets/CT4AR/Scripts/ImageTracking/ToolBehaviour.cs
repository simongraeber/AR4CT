using UnityEngine;
using TMPro;

namespace CT4AR.AR
{
    /// <summary>
    /// Attached to the tool prefab spawned on the ToolImage.
    /// Measures the distance from the tool tip (with adjustable offset) to a
    /// world-space target point and displays it on a UI Text element.
    /// </summary>
    public class ToolBehaviour : MonoBehaviour
    {
        // ──────────────────────── Inspector Fields ────────────────────────

        [Header("Tip Offset")]
        [Tooltip("Local-space offset from this transform to the actual tool tip.")]
        public Vector3 tipOffset = Vector3.zero;

        [Header("Tip Indicator")]
        [Tooltip("Child object that visualises where the tool tip is. " +
                 "Will be moved to match tipOffset every frame.")]
        [SerializeField] private Transform tipIndicator;

        [Header("Distance Display")]
        [Tooltip("TextMeshPro component that shows the distance in cm.")]
        [SerializeField] private TMP_Text distanceText;

        [Header("Color Gradient")]
        [Tooltip("Distance (in metres) considered 'far'. Maps to the far-end of the gradient.")]
        [SerializeField] private float maxColorDistance = 0.20f;

        [Tooltip("Color when distance is 0 (at the target).")]
        [SerializeField] private Color closeColor = Color.green;

        [Tooltip("Color when distance >= maxColorDistance.")]
        [SerializeField] private Color farColor = Color.red;

        [Header("Target Reached")]
        [Tooltip("Distance threshold (in metres) to consider the target reached.")]
        [SerializeField] private float targetReachedThreshold = 0.01f;

        [Tooltip("UI element activated when the tool tip is within the threshold.")]
        [SerializeField] private GameObject targetReachedUI;

        [Header("Audio")]
        [Tooltip("Sound played once when the target is first reached.")]
        [SerializeField] private AudioClip targetReachedClip;

        [Tooltip("AudioSource used to play the clip. If left empty one will be added automatically.")]
        [SerializeField] private AudioSource audioSource;

        // ──────────────────────── Runtime State ───────────────────────────

        /// <summary>
        /// World-space position the tool should reach.
        /// Set at runtime by <see cref="DynamicImageTracker"/>.
        /// </summary>
        [HideInInspector] public Transform targetPoint;

        private bool _wasReached;

        // ──────────────────────── Unity Callbacks ─────────────────────────

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null && targetReachedClip != null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Update()
        {
            // Keep the indicator at the tip
            if (tipIndicator != null)
                tipIndicator.localPosition = tipOffset;

            if (targetPoint == null)
            {
                if (distanceText != null)
                    distanceText.text = "—";
                return;
            }

            // Calculate world-space tip position
            Vector3 tipWorld = transform.TransformPoint(tipOffset);

            // Distance in metres → display in cm
            float distanceM  = Vector3.Distance(tipWorld, targetPoint.position);
            float distanceCm = distanceM * 100f;

            // Update text
            if (distanceText != null)
            {
                distanceText.text = $"{distanceCm:F1} cm";

                // Continuous colour lerp
                float t = Mathf.Clamp01(distanceM / maxColorDistance);
                distanceText.color = Color.Lerp(closeColor, farColor, t);
            }

            // Target reached — trigger once only
            if (!_wasReached && distanceM <= targetReachedThreshold)
            {
                _wasReached = true;

                if (targetReachedUI != null)
                    targetReachedUI.SetActive(true);

                if (targetReachedClip != null && audioSource != null)
                    audioSource.PlayOneShot(targetReachedClip);
            }
        }
    }
}
