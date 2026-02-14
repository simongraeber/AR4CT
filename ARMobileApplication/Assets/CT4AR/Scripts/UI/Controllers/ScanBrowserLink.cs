using UnityEngine;

namespace CT4AR.UI
{
    /// <summary>
    /// The type of scan URL to open.
    /// </summary>
    public enum ScanLinkType
    {
        /// <summary>Opens the scan web page: {baseUrl}/scans/{id}</summary>
        ScanPage,
        /// <summary>Opens the printable PDF: {apiBaseUrl}/scans/{id}/print.pdf</summary>
        PrintPdf,
    }

    /// <summary>
    /// Sets the URL on an <see cref="OpenBrowser"/> component to point to a scan resource.
    /// Choose the link type in the Inspector, then call <see cref="SetScanId"/> with the
    /// scan ID to configure the URL.
    /// </summary>
    public class ScanBrowserLink : MonoBehaviour
    {
        [SerializeField] private OpenBrowser openBrowser;

        [Tooltip("Which URL to open when the button is pressed.")]
        [SerializeField] private ScanLinkType linkType = ScanLinkType.ScanPage;

        [Tooltip("Base URL for the scan web page.")]
        [SerializeField] private string baseUrl = "https://ar4ct.com";

        [Tooltip("Base URL for the API (used for print PDF, etc.).")]
        [SerializeField] private string apiBaseUrl = "https://api.ar4ct.com";

        /// <summary>
        /// Updates the browser URL based on the configured <see cref="linkType"/>.
        /// </summary>
        public void SetScanId(string id)
        {
            if (openBrowser == null)
            {
                Debug.LogWarning("[CT4AR] ScanBrowserLink: No OpenBrowser reference assigned.");
                return;
            }

            string url = linkType switch
            {
                ScanLinkType.ScanPage => $"{baseUrl.TrimEnd('/')}/scans/{id}",
                ScanLinkType.PrintPdf => $"{apiBaseUrl.TrimEnd('/')}/scans/{id}/print.pdf",
                _ => $"{baseUrl.TrimEnd('/')}/scans/{id}",
            };

            openBrowser.SetURL(url);
        }
    }
}
