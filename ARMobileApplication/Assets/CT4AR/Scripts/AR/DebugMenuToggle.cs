using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace CT4AR.AR
{
    /// <summary>
    /// Standalone component that shows/hides the <see cref="ARDebugMenu"/>.
    /// Wire a <see cref="CT4AR.UI.ToggleButton"/>'s <c>onValueChanged(bool)</c>
    /// to <see cref="SetVisible"/> so the button handles the action directly.
    /// </summary>
    public class DebugMenuToggle : MonoBehaviour
    {
        [Tooltip("The ARDebugMenu to toggle.")]
        [SerializeField] private ARDebugMenu arDebugMenu;

        [Tooltip("Initial debug menu visibility.")]
        [SerializeField] private bool debugMenuVisible;

        [Header("Status Indicator")]
        [Tooltip("Optional slider whose value reflects the current state (1 = visible, 0 = hidden).")]
        [SerializeField] private Slider statusSlider;

        private float _savedPlanesButtonValue;

        /// <summary>True when the debug menu is currently visible.</summary>
        public bool DebugMenuVisible => debugMenuVisible;

        private void Start()
        {
            // Make sure the debug menu starts in the correct state.
            if (arDebugMenu != null)
                arDebugMenu.gameObject.SetActive(debugMenuVisible);

            UpdateSlider();
        }

        /// <summary>
        /// Sets debug menu visibility. Call from ToggleButton.onValueChanged.
        /// </summary>
        public void SetVisible(bool visible)
        {
            debugMenuVisible = visible;

            if (arDebugMenu == null) return;

            // Work around ARDebugMenu bug: enabling the menu resets the plane
            // visualizer toggles. Capture/restore the value across the transition.
            if (visible)
            {
                arDebugMenu.gameObject.SetActive(true);
                if (arDebugMenu.showPlanesButton.value != _savedPlanesButtonValue)
                    arDebugMenu.showPlanesButton.value = _savedPlanesButtonValue;
            }
            else
            {
                _savedPlanesButtonValue = arDebugMenu.showPlanesButton.value;
                if (_savedPlanesButtonValue == 1f)
                    arDebugMenu.showPlanesButton.value = 0f;

                arDebugMenu.gameObject.SetActive(false);
            }

            UpdateSlider();
        }

        private void UpdateSlider()
        {
            if (statusSlider != null)
                statusSlider.value = debugMenuVisible ? 1f : 0f;
        }
    }
}
