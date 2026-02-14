using TMPro;
using UnityEngine;

namespace CT4AR.UI
{
    /// <summary>
    /// Displays the scan ID from a deep link in a TextMeshPro label.
    /// Wire DeepLinkHandler.onDeepLinkActivated → this.SetScanId
    /// </summary>
    public class ScanIdDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text scanIdLabel;
        public void SetScanId(string scanId)
        {
            if (scanIdLabel != null)
                scanIdLabel.text = scanId;
        }
    }
}
