using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Templates.AR;

namespace CT4AR.AR
{
    /// <summary>
    /// Standalone component that controls AR plane visualization.
    /// Wire a <see cref="CT4AR.UI.ToggleButton"/>'s <c>onValueChanged(bool)</c>
    /// to <see cref="SetVisible"/> so the button handles the action directly.
    /// </summary>
    public class PlaneVisibilityToggle : MonoBehaviour
    {
        [Tooltip("The ARPlaneManager in the scene.")]
        [SerializeField] private ARPlaneManager planeManager;

        [Tooltip("Use fading animation when toggling planes.")]
        [SerializeField] private bool useFading = true;

        [Tooltip("Initial visibility state.")]
        [SerializeField] private bool planesVisible = true;

        [Header("Status Indicator")]
        [Tooltip("Optional slider whose value reflects the current visibility state (1 = visible, 0 = hidden).")]
        [SerializeField] private Slider statusSlider;

        private readonly List<ARPlane> _planes = new();
        private readonly Dictionary<ARPlane, ARPlaneMeshVisualizer> _visualizers = new();
        private readonly Dictionary<ARPlane, ARPlaneMeshVisualizerFader> _faders = new();

        /// <summary>True when planes are currently visible.</summary>
        public bool PlanesVisible => planesVisible;

        private void OnEnable()
        {
            if (planeManager != null)
                planeManager.trackablesChanged.AddListener(OnPlaneChanged);
        }

        private void OnDisable()
        {
            if (planeManager != null)
                planeManager.trackablesChanged.RemoveListener(OnPlaneChanged);
        }

        /// <summary>
        /// Sets plane visibility. Call from ToggleButton.onValueChanged.
        /// </summary>
        public void SetVisible(bool visible)
        {
            planesVisible = visible;
            ApplyVisibility(visible);
            UpdateSlider();
        }

        private void Start()
        {
            UpdateSlider();
        }

        private void UpdateSlider()
        {
            if (statusSlider != null)
                statusSlider.value = planesVisible ? 1f : 0f;
        }

        private void ApplyVisibility(bool visible)
        {
            foreach (var plane in _planes)
            {
                if (_visualizers.TryGetValue(plane, out var vis))
                    vis.enabled = useFading || visible;

                if (_faders.TryGetValue(plane, out var fader))
                {
                    if (useFading)
                        fader.visualizeSurfaces = visible;
                    else
                        fader.SetVisualsImmediate(1f);
                }
            }
        }

        private void OnPlaneChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            foreach (var plane in args.added)
            {
                _planes.Add(plane);

                if (plane.TryGetComponent<ARPlaneMeshVisualizer>(out var vis))
                {
                    _visualizers[plane] = vis;
                    if (!useFading) vis.enabled = planesVisible;
                }

                if (!plane.TryGetComponent<ARPlaneMeshVisualizerFader>(out var fader))
                    fader = plane.gameObject.AddComponent<ARPlaneMeshVisualizerFader>();

                _faders[plane] = fader;
                fader.visualizeSurfaces = planesVisible;
            }

            foreach (var removed in args.removed)
            {
                var p = removed.Value;
                if (p == null) continue;
                _planes.Remove(p);
                _visualizers.Remove(p);
                _faders.Remove(p);
            }

            // Resync if counts drift
            if (planeManager.trackables.count != _planes.Count)
            {
                _planes.Clear();
                _visualizers.Clear();
                _faders.Clear();

                foreach (var plane in planeManager.trackables)
                {
                    _planes.Add(plane);

                    if (plane.TryGetComponent<ARPlaneMeshVisualizer>(out var vis))
                    {
                        _visualizers[plane] = vis;
                        if (!useFading) vis.enabled = planesVisible;
                    }

                    if (!plane.TryGetComponent<ARPlaneMeshVisualizerFader>(out var fader))
                        fader = plane.gameObject.AddComponent<ARPlaneMeshVisualizerFader>();

                    _faders[plane] = fader;
                    fader.visualizeSurfaces = planesVisible;
                }
            }
        }
    }
}
