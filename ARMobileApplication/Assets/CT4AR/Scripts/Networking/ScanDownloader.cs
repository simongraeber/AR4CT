using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using TMPro;
using CT4AR.Utils;

namespace CT4AR.Networking
{
    /// <summary>
    /// Downloads a 3D scan (FBX) and its reference image from the CT4AR API.
    /// Call <see cref="StartDownload"/> to begin. While downloading the
    /// <see cref="downloadUI"/> is shown; on failure <see cref="errorUI"/>
    /// is activated and the error message is written to <see cref="errorLabel"/>.
    /// </summary>
    public class ScanDownloader : MonoBehaviour
    {
        [Header("Scan")]
        [Tooltip("The scan UUID to download (e.g. f0da0ef5-3c13-4978-b5b2-c2517225cfd5).")]
        public string scanID;

        [Header("Config")]
        [Tooltip("Optional – overrides the default API base URL from CT4ARConfig.")]
        [SerializeField] private CT4ARConfig config;

        [Header("UI")]
        [Tooltip("Activated while the download is in progress.")]
        [SerializeField] private GameObject downloadUI;

        [Tooltip("Activated when the download fails.")]
        [SerializeField] private GameObject errorUI;

        [Tooltip("Label inside the error UI where the error message is displayed.")]
        [SerializeField] private TMP_Text errorLabel;

        [Header("Events")]
        [Tooltip("Fired when the download completes successfully. Parameter = local file path.")]
        public UnityEvent<string> onDownloadComplete;

        [Tooltip("Fired when the download fails. Parameter = error message.")]
        public UnityEvent<string> onDownloadFailed;

        [Tooltip("Fired when the reference image has been downloaded. Parameter = Texture2D.")]
        public UnityEvent<Texture2D> onReferenceImageDownloaded;

        [Tooltip("Fired when the point data has been downloaded. Parameter = ScanPointData.")]
        public UnityEvent<ScanPointData> onPointDataDownloaded;

        private UnityWebRequest _activeRequest;

        /// <summary>The downloaded reference image, or null if not yet available.</summary>
        public Texture2D ReferenceImage { get; private set; }

        /// <summary>The downloaded point data, or null if not yet available.</summary>
        public ScanPointData PointData { get; private set; }

        /// <summary>True while a download is in progress.</summary>
        public bool IsDownloading { get; private set; }

        /// <summary>
        /// Sets the scan ID without starting a download.
        /// </summary>
        public void SetScanID(string id)
        {
            scanID = id;
        }

        /// <summary>
        /// Starts downloading the FBX for the current <see cref="scanID"/>.
        /// </summary>
        public void StartDownload()
        {
            if (string.IsNullOrEmpty(scanID))
            {
                ShowError("Scan ID is empty.");
                return;
            }

            if (IsDownloading)
            {
                Debug.LogWarning("[CT4AR] Download already in progress.");
                return;
            }

            // Hide previous error UI
            if (errorUI != null) errorUI.SetActive(false);

            // Show download UI
            if (downloadUI != null) downloadUI.SetActive(true);

            string baseUrl = config != null ? config.apiBaseUrl : "https://api.ar4ct.com";
            string fbxUrl = $"{baseUrl.TrimEnd('/')}/scans/{scanID}/fbx";
            string imageUrl = $"{baseUrl.TrimEnd('/')}/scans/{scanID}/image.png";
            string bundleUrl = $"{baseUrl.TrimEnd('/')}/scans/{scanID}/bundle";

            Debug.Log("[CT4AR] Starting FBX download: " + fbxUrl);
            Debug.Log("[CT4AR] Starting image download: " + imageUrl);
            Debug.Log("[CT4AR] Starting bundle download: " + bundleUrl);
            StartCoroutine(DownloadRoutine(fbxUrl, imageUrl, bundleUrl));
        }

        /// <summary>
        /// Convenience overload – sets the scan ID then starts the download.
        /// </summary>
        public void StartDownload(string id)
        {
            scanID = id;
            StartDownload();
        }

        /// <summary>
        /// Cancels the current download, if any.
        /// </summary>
        public void CancelDownload()
        {
            if (_activeRequest != null && !_activeRequest.isDone)
            {
                _activeRequest.Abort();
                _activeRequest.Dispose();
                _activeRequest = null;
            }

            IsDownloading = false;
            if (downloadUI != null) downloadUI.SetActive(false);
        }

        private System.Collections.IEnumerator DownloadRoutine(string fbxUrl, string imageUrl, string bundleUrl)
        {
            IsDownloading = true;

            // --- Download reference image ---
            Debug.Log("[CT4AR] Downloading reference image…");
            using (UnityWebRequest imgRequest = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                _activeRequest = imgRequest;
                yield return imgRequest.SendWebRequest();

                if (imgRequest.result != UnityWebRequest.Result.Success)
                {
                    string warnMsg = $"Reference image download failed: {imgRequest.error} (HTTP {imgRequest.responseCode})";
                    Debug.LogWarning("[CT4AR] " + warnMsg);
                    // Non-fatal – continue with FBX download
                }
                else
                {
                    ReferenceImage = DownloadHandlerTexture.GetContent(imgRequest);
                    Debug.Log("[CT4AR] Reference image downloaded.");
                    onReferenceImageDownloaded?.Invoke(ReferenceImage);
                }
            }

            // --- Download bundle (point in FBX model space) ---
            Debug.Log("[CT4AR] Downloading bundle data\u2026");
            using (UnityWebRequest bundleRequest = UnityWebRequest.Get(bundleUrl))
            {
                _activeRequest = bundleRequest;
                yield return bundleRequest.SendWebRequest();

                if (bundleRequest.result != UnityWebRequest.Result.Success)
                {
                    string warnMsg = $"Bundle download failed: {bundleRequest.error} (HTTP {bundleRequest.responseCode})";
                    Debug.LogWarning("[CT4AR] " + warnMsg);
                }
                else
                {
                    try
                    {
                        PointData = JsonUtility.FromJson<ScanPointData>(bundleRequest.downloadHandler.text);
                        Debug.Log("[CT4AR] Bundle data downloaded (point in FBX space).");
                        onPointDataDownloaded?.Invoke(PointData);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("[CT4AR] Failed to parse bundle data: " + e.Message);
                    }
                }
            }

            // --- Download FBX model ---
            string fileName = $"{scanID}.fbx";
            string localPath = Path.Combine(Application.persistentDataPath, fileName);

            using (UnityWebRequest request = UnityWebRequest.Get(fbxUrl))
            {
                _activeRequest = request;
                request.downloadHandler = new DownloadHandlerFile(localPath) { removeFileOnAbort = true };

                yield return request.SendWebRequest();

                _activeRequest = null;
                IsDownloading = false;

                // Hide download UI
                if (downloadUI != null) downloadUI.SetActive(false);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string errorMsg = $"Download failed: {request.error} (HTTP {request.responseCode})";
                    Debug.LogError("[CT4AR] " + errorMsg);
                    ShowError(errorMsg);
                    onDownloadFailed?.Invoke(errorMsg);
                }
                else
                {
                    Debug.Log("[CT4AR] FBX download complete: " + localPath);
                    onDownloadComplete?.Invoke(localPath);
                }
            }
        }

        private void ShowError(string message)
        {
            if (errorUI != null) errorUI.SetActive(true);
            if (errorLabel != null) errorLabel.text = message;
        }

        private void OnDestroy()
        {
            CancelDownload();
        }
    }
}
