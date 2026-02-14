using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CT4AR.UI
{
    /// <summary>
    /// Panel with three input fields (X, Y, Z) that control the
    /// <see cref="CT4AR.AR.ToolBehaviour.tipOffset"/> at runtime.
    /// On activation the panel searches the scene for a <see cref="CT4AR.AR.ToolBehaviour"/>.
    /// If none is found, the input fields are disabled and a warning is shown.
    /// Changes are applied live as the user types. A Restore button resets to
    /// the values that were active when the panel was opened.
    /// </summary>
    public class ToolAxisOffsetPanel : MonoBehaviour
    {
        [Header("Input Fields")]
        [Tooltip("Input field for the X axis offset.")]
        [SerializeField] private TMP_InputField inputX;

        [Tooltip("Input field for the Y axis offset.")]
        [SerializeField] private TMP_InputField inputY;

        [Tooltip("Input field for the Z axis offset.")]
        [SerializeField] private TMP_InputField inputZ;

        [Header("Restore")]
        [Tooltip("Button that restores the offset to the values from when the panel was opened.")]
        [SerializeField] private Button restoreButton;

        [Header("Warning")]
        [Tooltip("Text shown when no ToolBehaviour is found in the scene.")]
        [SerializeField] private GameObject warningText;

        private CT4AR.AR.ToolBehaviour _toolBehaviour;
        private Vector3 _initialOffset;

        private void OnEnable()
        {
            _toolBehaviour = FindAnyObjectByType<CT4AR.AR.ToolBehaviour>();

            if (_toolBehaviour != null)
            {
                _initialOffset = _toolBehaviour.tipOffset;
                SetFieldsInteractable(true);
                WriteFieldsFromOffset(_initialOffset);
                if (warningText != null) warningText.SetActive(false);
            }
            else
            {
                SetFieldsInteractable(false);
                ClearFields();
                if (warningText != null) warningText.SetActive(true);
            }

            if (inputX != null) inputX.onValueChanged.AddListener(OnFieldChanged);
            if (inputY != null) inputY.onValueChanged.AddListener(OnFieldChanged);
            if (inputZ != null) inputZ.onValueChanged.AddListener(OnFieldChanged);

            if (restoreButton != null)
            {
                restoreButton.interactable = _toolBehaviour != null;
                restoreButton.onClick.AddListener(RestoreOffset);
            }
        }

        private void OnDisable()
        {
            if (inputX != null) inputX.onValueChanged.RemoveListener(OnFieldChanged);
            if (inputY != null) inputY.onValueChanged.RemoveListener(OnFieldChanged);
            if (inputZ != null) inputZ.onValueChanged.RemoveListener(OnFieldChanged);

            if (restoreButton != null)
                restoreButton.onClick.RemoveListener(RestoreOffset);
        }

        private void OnFieldChanged(string _)
        {
            if (_toolBehaviour == null) return;

            float x = ParseField(inputX, _toolBehaviour.tipOffset.x);
            float y = ParseField(inputY, _toolBehaviour.tipOffset.y);
            float z = ParseField(inputZ, _toolBehaviour.tipOffset.z);

            _toolBehaviour.tipOffset = new Vector3(x, y, z);
        }

        /// <summary>
        /// Restores the offset to the values captured when the panel was opened.
        /// </summary>
        public void RestoreOffset()
        {
            if (_toolBehaviour == null) return;

            _toolBehaviour.tipOffset = _initialOffset;
            WriteFieldsFromOffset(_initialOffset);
        }

        private void WriteFieldsFromOffset(Vector3 offset)
        {
            if (inputX != null) inputX.text = offset.x.ToString("F3");
            if (inputY != null) inputY.text = offset.y.ToString("F3");
            if (inputZ != null) inputZ.text = offset.z.ToString("F3");
        }

        private void ClearFields()
        {
            if (inputX != null) inputX.text = "";
            if (inputY != null) inputY.text = "";
            if (inputZ != null) inputZ.text = "";
        }

        private void SetFieldsInteractable(bool interactable)
        {
            if (inputX != null) inputX.interactable = interactable;
            if (inputY != null) inputY.interactable = interactable;
            if (inputZ != null) inputZ.interactable = interactable;
        }

        private static float ParseField(TMP_InputField field, float fallback)
        {
            if (field == null) return fallback;
            return float.TryParse(field.text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float result)
                ? result
                : fallback;
        }
    }
}
