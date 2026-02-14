using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.Jobs;

using TriLibCore;
using TriLibCore.General;

namespace CT4AR.AR
{
    /// <summary>
    /// Receives the downloaded reference image and FBX from ScanDownloader,
    /// builds a runtime image library, and places the loaded model on the
    /// tracked image.
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class DynamicImageTracker : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The ScanDownloader that provides the image & model.")]
        [SerializeField] private CT4AR.Networking.ScanDownloader scanDownloader;

        [Header("Settings")]
        [Tooltip("Physical width of the printed marker in meters.")]
        [SerializeField] private float markerWidthInMeters = 0.15f;

        [Tooltip("Scale applied to the loaded model (e.g. 0.01 to convert cm to m).")]
        [SerializeField] private float modelScale = 0.01f;

        [Tooltip("Local position offset of the model relative to the tracked image.")]
        [SerializeField] private Vector3 modelOffset = Vector3.zero;

        [Header("UI")]
        [Tooltip("Hint UI shown when tracking is ready, hidden once the image is found.")]
        [SerializeField] private GameObject scanHintUI;

        [Tooltip("OpenBrowser component on the hint UI to set the print PDF URL.")]
        [SerializeField] private CT4AR.UI.OpenBrowser printPdfBrowser;

        [Header("Tool Tracking")]
        [Tooltip("XR Reference Image Library containing the ToolImage. Assign in the Inspector.")]
        [SerializeField] private XRReferenceImageLibrary serializedImageLibrary;

        [Tooltip("Prefab spawned when the ToolImage is detected.")]
        [SerializeField] private GameObject toolPrefab;

        [Header("Point Marker")]
        [Tooltip("Prefab instantiated at the downloaded point position. Not affected by model scale.")]
        [SerializeField] private GameObject pointMarkerPrefab;

        private const string ToolImageName = "ToolImage";

        private ARTrackedImageManager _imageManager;
        private GameObject _loadedModel;
        private bool _referenceImageReady;
        private bool _modelReady;
        private string _trackedImageName;
        private bool _imageFound;
        private CT4AR.Networking.ScanPointData _pointData;

        private readonly Dictionary<string, GameObject> _spawnedObjects = new();
        private Transform _pointMarkerTransform;

        private void Awake()
        {
            _imageManager = GetComponent<ARTrackedImageManager>();
            _imageManager.enabled = false;
        }

        private void OnEnable()
        {
            scanDownloader.onReferenceImageDownloaded.AddListener(OnReferenceImageDownloaded);
            scanDownloader.onDownloadComplete.AddListener(OnFBXDownloaded);
            scanDownloader.onPointDataDownloaded.AddListener(OnPointDataDownloaded);
            _imageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }

        private void OnDisable()
        {
            scanDownloader.onReferenceImageDownloaded.RemoveListener(OnReferenceImageDownloaded);
            scanDownloader.onDownloadComplete.RemoveListener(OnFBXDownloaded);
            scanDownloader.onPointDataDownloaded.RemoveListener(OnPointDataDownloaded);

            _imageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        }

        private void OnReferenceImageDownloaded(Texture2D refImage)
        {
            Debug.Log("[DynamicImageTracker] Reference image received, building runtime library…");

            _imageManager.referenceLibrary = _imageManager.CreateRuntimeLibrary(serializedImageLibrary);

            if (_imageManager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLib)
            {
                _trackedImageName = scanDownloader.scanID;

                var jobState = mutableLib.ScheduleAddImageWithValidationJob(
                    refImage,
                    _trackedImageName,
                    markerWidthInMeters
                );
                jobState.jobHandle.Complete();

                _referenceImageReady = true;
                Debug.Log("[DynamicImageTracker] Reference image added to runtime library.");
                TryActivateTracking();
            }
            else
            {
                Debug.LogError("[DynamicImageTracker] Platform does not support mutable image libraries!");
            }
        }

        private void OnFBXDownloaded(string localFbxPath)
        {
            Debug.Log($"[DynamicImageTracker] FBX downloaded to {localFbxPath}, loading model…");

            var assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
            AssetLoader.LoadModelFromFile(
                localFbxPath,
                onLoad: (assetLoaderContext) =>
                {
                    _loadedModel = assetLoaderContext.RootGameObject;
                    _loadedModel.SetActive(false);
                    _modelReady = true;
                    Debug.Log("[DynamicImageTracker] Model loaded via TriLib.");
                    TryActivateTracking();
                },
                onError: (contextualizedError) =>
                {
                    Debug.LogError($"[DynamicImageTracker] TriLib load error: {contextualizedError}");
                },
                assetLoaderOptions: assetLoaderOptions
            );
        }

        private void OnPointDataDownloaded(CT4AR.Networking.ScanPointData pointData)
        {
            _pointData = pointData;
            Debug.Log($"[DynamicImageTracker] Point data received: ({pointData.point.x}, {pointData.point.y}, {pointData.point.z})");
        }

        private void TryActivateTracking()
        {
            if (_referenceImageReady && _modelReady)
            {
                _imageManager.enabled = true;

                if (scanHintUI != null)
                    scanHintUI.SetActive(true);

                if (printPdfBrowser != null)
                    printPdfBrowser.SetURL($"https://api.ar4ct.com/scans/{scanDownloader.scanID}/print.pdf");

                Debug.Log("[DynamicImageTracker] ARTrackedImageManager enabled — tracking is live.");
            }
        }

        private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            foreach (var trackedImage in args.added)
                HandleTrackedImage(trackedImage);

            foreach (var trackedImage in args.updated)
                HandleTrackedImage(trackedImage);

            foreach (var removed in args.removed)
            {
                if (_spawnedObjects.TryGetValue(
                        removed.Value.referenceImage.name, out var obj))
                {
                    obj.SetActive(false);
                }
            }
        }

        private void HandleTrackedImage(ARTrackedImage trackedImage)
        {
            string imageName = trackedImage.referenceImage.name;

            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                if (!_imageFound)
                {
                    _imageFound = true;
                    if (scanHintUI != null) scanHintUI.SetActive(false);
                }

                if (!_spawnedObjects.ContainsKey(imageName))
                {
                    if (imageName == ToolImageName)
                    {
                        if (toolPrefab != null)
                        {
                            var tool = Instantiate(toolPrefab, trackedImage.transform);
                            tool.transform.localPosition = Vector3.zero;
                            _spawnedObjects[imageName] = tool;

                            // Wire ToolBehaviour to the point marker if it already exists
                            var tb = tool.GetComponent<ToolBehaviour>();
                            if (tb != null && _pointMarkerTransform != null)
                                tb.targetPoint = _pointMarkerTransform;
                        }
                    }
                    else
                    {
                        var instance = Instantiate(_loadedModel,
                            trackedImage.transform.position,
                            trackedImage.transform.rotation);

                        instance.transform.localScale = Vector3.one * modelScale;
                        instance.SetActive(true);
                        instance.transform.SetParent(trackedImage.transform);
                        instance.transform.localPosition = modelOffset;
                        _spawnedObjects[imageName] = instance;

                        if (_pointData?.point != null && pointMarkerPrefab != null)
                        {
                            Vector3 pointLocalPos = _pointData.point.ToVector3() * modelScale;
                            var marker = Instantiate(pointMarkerPrefab, instance.transform);
                            marker.transform.localPosition = pointLocalPos / modelScale;
                            _pointMarkerTransform = marker.transform;

                            // Wire ToolBehaviour if the tool was already spawned
                            if (_spawnedObjects.TryGetValue(ToolImageName, out var toolObj))
                            {
                                var tb = toolObj.GetComponent<ToolBehaviour>();
                                if (tb != null)
                                    tb.targetPoint = _pointMarkerTransform;
                            }
                        }
                    }
                }
                else
                {
                    _spawnedObjects[imageName].SetActive(true);
                }
            }
        }
    }
}
