using System;
using TriLibCore.General;
using TriLibCore.Mappers;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using MaterialMapper = TriLibCore.Mappers.MaterialMapper;
using TextureCompressionQuality = TriLibCore.General.TextureCompressionQuality;

namespace TriLibCore
{
    /// <summary>Represents a series of Model loading settings, like Unity Model Importer settings.</summary>
    [CreateAssetMenu(menuName = "TriLib/Asset Loader Options/Empty Asset Loader Options")]
    public class AssetLoaderOptions : ScriptableObject
    {
        //Model
        /// <summary>
        /// Turn on this flag to use the file's original scale.
        /// </summary>
        public bool UseFileScale = true;

        /// <summary>
        /// Model scale multiplier.
        /// </summary>
        public float ScaleFactor = 1f;

        /// <summary>
        /// Turn on this field to sort the Model hierarchy by name.
        /// </summary>
        public bool SortHierarchyByName = true;

        /// <summary>
        /// Turn on this field to apply the visibility property to Mesh Renderers/Skinned Mesh Renderers.
        /// </summary>
        public bool ImportVisibility = true;

        /// <summary>
        /// Turn on this field to import the Model as a static Game Object.
        /// </summary>
        public bool Static;

        /// <summary>
        /// Turn on this field to add the Asset Unloader Component to the loaded Game Object, which deallocates resources automatically.
        /// </summary>
        public bool AddAssetUnloader = true;

        //Meshes
        /// <summary>
        /// Turn on this field to import Model Meshes.
        /// </summary>
        public bool ImportMeshes = true;

        /// <summary>
        /// Turn on this field to limit bone weights to 4 per bone.
        /// </summary>
        public bool LimitBoneWeights = true;

        /// <summary>
        /// Turn on this field to leave imported Meshes readable.
        /// </summary>
        public bool ReadEnabled = false;

        /// <summary>
        /// Turn on this field to mark created meshes as dynamic.
        /// </summary>
        public bool MarkMeshesAsDynamic;

        /// <summary>
        /// Turn on this field to optimize imported Meshes for GPU access.
        /// </summary>
        public bool OptimizeMeshes = true;

        /// <summary>
        /// Turn on this field to generate Colliders for imported Meshes.
        /// </summary>
        public bool GenerateColliders;

        /// <summary>
        /// Turn on this field to generate convex Colliders when the GenerateColliders field is enabled.
        /// </summary>
        public bool ConvexColliders;

        /// <summary>
        /// Turn on this field to import Mesh Blend Shapes.
        /// </summary>
        public bool ImportBlendShapes = true;

        /// <summary>
        /// Turn on this field to import Mesh colors.
        /// </summary>
        public bool ImportColors = true;

        //Geometry
        /// <summary>
        /// Mesh index format (16 or 32 bits).
        /// </summary>
        public IndexFormat IndexFormat = IndexFormat.UInt16;

        /// <summary>
        /// Defines the initial screen relative transition height when creating LOD Groups.
        /// </summary>
        public float LODScreenRelativeTransitionHeightBase = 0.75f;

        /// <summary>
        /// Turn on this field to maintain Mesh quads. (Useful for DX11 tessellation)
        /// </summary>
        public bool KeepQuads;

        /// <summary>
        /// Turn on this field to import Mesh normals. If disabled, normals will be calculated instead.
        /// </summary>
        public bool ImportNormals = true;

        /// <summary>
        /// Turn off this field to disable Mesh normals generation.
        /// </summary>
        public bool GenerateNormals = true;

        /// <summary>
        /// Turn off this field to disable Mesh tangents generation.
        /// </summary>
        public bool GenerateTangents = true;

        /// <summary>
        /// Normals calculation smoothing angle.
        /// </summary>
        public float SmoothingAngle = 60f;

        /// <summary>
        /// Turn on this field to import Mesh Blend Shape normals.
        /// </summary>
        public bool ImportBlendShapeNormals;

        /// <summary>
        /// Turn on this field to calculate Mesh Blend Shape normals when none can be imported.
        /// </summary>
        public bool CalculateBlendShapeNormals;

        /// <summary>
        /// Turn on this field to import Mesh tangents. If disabled, tangents will be calculated instead.
        /// </summary>
        public bool ImportTangents;

        /// <summary>
        /// Turn on this field to swap Mesh UVs. (uv1 into uv2)
        /// </summary>
        public bool SwapUVs;

        //Materials
        /// <summary>
        /// Turn on this field to import Materials.
        /// </summary>
        public bool ImportMaterials = true;

        /// <summary>
        /// Mappers used to create suitable Unity Materials from original Materials.
        /// </summary>
        public MaterialMapper[] MaterialMappers;

        /// <summary>
        /// Turn on this field to import Textures.
        /// </summary>
        public bool ImportTextures = true;

        /// <summary>
        /// Turn on this field to scan Textures for alpha-blended pixels in order to generate transparent Materials.
        /// </summary>
        public bool ScanForAlphaPixels = true;

        /// <summary>
        /// Chooses the way TriLib creates alpha materials. The options are:
        /// None: Does not create any alpha material and uses opaque materials instead.
        /// Cutout: Creates cutout alpha materials where applicable.
        /// Transparent: Creates transparent (alpha) materials where applicable.
        /// Cutout + Transparent: Creates both materials and uses the second one as a copy from the semi-transparent mesh.
        /// This field is ignored when the Material Mapper uses a Shader Variant Collection.
        /// </summary>
        public AlphaMaterialMode AlphaMaterialMode = AlphaMaterialMode.CutoutAndTransparent;

        /// <summary>
        /// Turn on this field to create double-sided Materials (TriLib does that by duplicating the original Meshes).
        /// </summary>
        [Obsolete("This method has issues and has been removed temporarily.")]
        public bool DoubleSidedMaterials;

        /// <summary>
        /// Mappers used to find native Texture Streams from custom sources.
        /// </summary>
        public TextureMapper[] TextureMappers;

        /// <summary>
        /// Texture compression to apply on loaded Textures.
        /// </summary>
        public TextureCompressionQuality TextureCompressionQuality = TextureCompressionQuality.Normal;

        /// <summary>
        /// Turn on this field to enable Textures mip-map generation.
        /// </summary>
        public bool GenerateMipmaps = true;

        /// <summary>
        /// Turn on this field to change the normal map channel order to ABBR instead of RGBA.
        /// </summary>
        public bool FixNormalMaps;

        //Rig
        /// <summary>
        /// Model rigging type.
        /// </summary>
        public AnimationType AnimationType = AnimationType.Legacy;

        /// <summary>
        /// Turn on this field to simplify animation curves.
        /// </summary>
        public bool SimplifyAnimations;

        /// <summary>
        /// Position simplification threshold. The smaller the values, the more precision.
        /// </summary>
        public float PositionThreshold = 0.01f;

        /// <summary>
        /// Rotation simplification threshold (in degrees). The smaller the values, the more precision.
        /// </summary>
        public float RotationThreshold = 0.01f;

        /// <summary>
        /// Scale simplification threshold. The smaller the values, the more precision.
        /// </summary>
        public float ScaleThreshold = 0.01f;

        /// <summary>
        /// Type of Avatar creation for the Model.
        /// </summary>
        public AvatarDefinitionType AvatarDefinition;

        /// <summary>
        /// Source Avatar to use when copying from another Avatar.
        /// </summary>
        public Avatar Avatar;

        /// <summary>
        /// Human Description used to create the humanoid Avatar, when the humanoid rigging type is selected.
        /// </summary>
        public General.HumanDescription HumanDescription;

        /// <summary>
        /// Mapper used to find the Model root bone.
        /// </summary>
        public RootBoneMapper RootBoneMapper;

        /// <summary>
        /// Mapper used to map the humanoid Avatar bones when the humanoid rigging type is selected.
        /// </summary>
        public HumanoidAvatarMapper HumanoidAvatarMapper;

        /// <summary>
        /// Mappers used to configure Lip-Sync Blend Shapes.
        /// </summary>
        public LipSyncMapper[] LipSyncMappers;

        /// <summary>
        /// Turn on this field to sample the loaded Model to the bind-pose when rigging.
        /// </summary>
        public bool SampleBindPose;

        /// <summary>
        /// Turn on this field to enforce the loaded Model to the T-pose when rigging.
        /// </summary>
        public bool EnforceTPose = true;

        /// <summary>
        /// Turn on this field to add an Animator when the AnimationType is set to Legacy.
        /// </summary>
        public bool EnforceAnimatorWithLegacyAnimations;

        /// <summary>
        /// Turn on this field to play Legacy Animation Clips automatically (The first available Clip will be played).
        /// </summary>
        public bool AutomaticallyPlayLegacyAnimations;

        /// <summary>
        /// Defines the FBX Rotation Animation Curve resampling frequency. (1 = every frame, 2 = every 2 frames, 3 = every 3 frames, and so on)
        /// </summary>
        public float ResampleFrequency = 4f;

        /// <summary>
        /// Default wrap-mode to apply to Animations.
        /// </summary>
        public WrapMode AnimationWrapMode = WrapMode.Loop;

        /// <summary>
        /// Mappers used to process Animation Clips.
        /// </summary>
        public AnimationClipMapper[] AnimationClipMappers;

        //External data
        /// <summary>
        /// Mapper used to find data Streams on external sources.
        /// </summary>
        public ExternalDataMapper ExternalDataMapper;

#if DEBUG
        /// <summary>
        /// Turn on this field to display Model loading warnings on the Console.
        /// </summary>
        public bool ShowLoadingWarnings;
#else
        public bool ShowLoadingWarnings
        {
            get
            {
                return false;
            }
            set
            {

            }
        }
#endif

        /// <summary>
        /// Turn on this field to close the Model loading Stream automatically.
        /// </summary>
        public bool CloseStreamAutomatically = true;

        /// <summary>
        /// Model loading timeout in seconds (0=disabled).
        /// </summary>
        public int Timeout = 180;

        /// <summary>
        /// Turn on this field to destroy the loaded Game Object automatically when there is any loading error.
        /// </summary>
        public bool DestroyOnError = true;

        /// <summary>
        /// Turn on this field to realign quaternion keys to ensure the shortest interpolation paths.
        /// </summary>
        public bool EnsureQuaternionContinuity = true;

        /// <summary>
        /// Turn on this field to use shader keywords on generated Materials.
        /// This field is useful when using Shader Variants to get more control over generated Materials.
        /// This field is ignored when enabling the UsesShaderVariantCollection field.
        /// </summary>
        public bool UseMaterialKeywords;

        /// <summary>
        /// Turn on this field to merge model duplicated vertices where possible.
        /// </summary>
        public bool MergeVertices = true;

        /// <summary>
        /// Turn on this field to set textures as no longer readable and release memory resources.
        /// </summary>
        public bool MarkTexturesNoLongerReadable = true;

        /// <summary>
        /// Turn on this field to use the built-in Unity normal calculator.
        /// Disabling this field allows TriLib to accept more texture file formats but uses more memory to load textures.
        /// </summary>
        public bool UseUnityNativeNormalCalculator = false;

        /// <summary>
        /// When this field is on, TriLib will also apply the gamma curve to the material colors.
        /// </summary>
        public bool ApplyGammaCurveToMaterialColors = true;

        /// <summary>
        /// Turn off this field to load textures as linear, instead of sRGB.
        /// </summary>
        public bool LoadTexturesAsSRGB = true;

        /// <summary>
        /// Mapper used to process User Properties from Models.
        /// </summary>
        public UserPropertiesMapper UserPropertiesMapper;

        /// <summary>
        /// Turn on this field to apply Textures offset and scaling.
        /// </summary>
        public bool ApplyTexturesOffsetAndScaling = true;

        /// <summary>
        /// Turn off this field to keep unused Textures.
        /// </summary>
        public bool DiscardUnusedTextures = true;

        /// <summary>
        /// Use this field to realign the Model pivot based on the given value.
        /// </summary>
        public PivotPosition PivotPosition;

        /// <summary>
        /// Turn on this field to enforce power-of-two resolution when loading textures (needed for texture compression and on some platforms).
        /// </summary>
        public bool ForcePowerOfTwoTextures = false;

        /// <summary>
        /// Use this field to limit texture resolution. Textures with resolutions higher than this value (when the value is not zero) will not be loaded.
        /// </summary>
        public int MaxTexturesResolution;

        /// <summary>
        /// Turn on this field to use the Unity built-in Texture loader instead of stb_image.
        /// </summary>
        public bool UseUnityNativeTextureLoader;

        /// <summary>
        /// Turn on this field to enable Cameras importing.
        /// </summary>
        public bool ImportCameras;

        /// <summary>
        /// Turn on this field to enable Lights importing.
        /// </summary>
        public bool ImportLights;

        /// <summary>
        /// Turn on this field to disable objects renaming.
        /// Remarks: this feature may break animation compatibility as they won't work with duplicate object names.
        /// </summary>
        public bool DisableObjectsRenaming;

        /// <summary>
        /// Turn on this field to merge single child models into a single GameObject.
        /// </summary>
        public bool MergeSingleChild;

        /// <summary>
        /// Turn on this field to set the unused Material Texture Properties to <c>null</c>.
        /// </summary>
        public bool SetUnusedTexturePropertiesToNull;

        /// <summary>
        /// Turn on this field to keep isolated vertices when loading the model (PLY and OBJ only).
        /// </summary>
        public bool LoadPointClouds;

        /// <summary>
        /// Turn off this field to disable embedded resource extraction. When this field is on, embedded textures and resources are extracted to disk and can work as a cache system.
        /// </summary>
        public bool ExtractEmbeddedData = false;

        /// <summary>
        /// Path to extract embedded resources.
        /// Keep in mind this is an absolute path.
        /// If this value is "null", Unity will use the "Persistent Data Path" to store the embedded data.
        /// </summary>
        public string EmbeddedDataExtractionPath;

        /// <summary>
        /// Change this field to define how TriLib will load files into memory before processing the file (When enabled, it decreases loading times but increases memory usage).
        /// </summary>
        public FileBufferingMode BufferizeFiles = FileBufferingMode.Always;

        /// <summary>
        /// Turn off this field to disable "Metallic/Smoothness/Specular/Roughness/Emission" automatic texture creation.
        /// When turned on, TriLib will emulate the Standard material's Metallic/Smoothness texture and assign a default white texture to the Emissive slot when appropriate.
        /// </summary>
        public bool ConvertMaterialTextures = true;

        /// <summary>
        /// Turn off this field to set the "Metallic/Smoothness/Specular/Roughness" texture sizes to the full original resolution.
        /// </summary>
        public bool ConvertMaterialTexturesUsingHalfRes = true;

        /// <summary>
        /// Turn on this field to disable polygon tessellation.
        /// </summary>
        public bool DisableTesselation;

        /// <summary>
        /// Turn off this field if you don't want TriLib to search for textures inside all folders where the model is located.
        /// </summary>
        public bool SearchTexturesRecursively = true;

        /// <summary>
        /// Turn on this field to add all available bones to every created SkinnedMeshRenderer.
        /// </summary>
        public bool AddAllBonesToSkinnedMeshRenderers;

        /// <summary>
        /// Turn on this field to enforce an alpha channel on texture creation.
        /// </summary>
        public bool EnforceAlphaChannelTextures = true;

        /// <summary>
        /// Use a custom NameMapper to define how the final GameObjects will be named based on the input model data.
        /// </summary>
        public NameMapper NameMapper;

        /// <summary>
        /// Turn on this field to make TriLib create materials for every loaded model, even those without an original material.
        /// </summary>
        public bool CreateMaterialsForAllModels;

        /// <summary>
        /// Turn off this field to disable TriLib coroutines. Coroutines ensure that model data loads with the fewest possible main thread stalls.
        /// </summary>
        public bool UseCoroutines = false;

        /// <summary>
        /// Use this field to set a maximum number of milliseconds a Coroutine can take without waiting for a main thread frame to execute.
        /// </summary>
        public long MaxCoroutineDelayInMS = 500;

        /// <summary>
        /// Use this field to set the maximum initial capacity of mesh vertex buffers. Vertex counts higher than that will use the default .NET exponential capacity-increasing method.
        /// <remarks>
        /// Higher values can make models load faster but may require significantly more memory. It is recommended that this field be left at the default value.
        /// Leave as 0 to let TriLib use the calculated maximum vertex capacity for the loaded models.
        /// </remarks>
        /// </summary>
        public int MaxVertexDataInitialCapacity = 196605;

        /// <summary>
        /// Turn off this field to disable GC collection after model loading.
        /// </summary>
        public bool CollectCG = false;

        /// <summary>
        /// Turn on this field to enable .NET Large Object Heap compaction (UWP only).
        /// </summary>
        public bool CompactHeap;

        /// <summary>
        /// When this field is disabled, TriLib will read the texture resolution before creating it, which could increase memory usage.
        /// The new texture loading method doesn't do that. This option is left here for compatibility reasons.
        /// </summary>
        public bool LoadTexturesAtOnce = true;

        /// <summary>
        /// Turn on this field to ensure Unity will create textures that can be sampled on the current platform.
        /// </summary>
        public bool GetCompatibleTextureFormat = true;

        /// <summary>
        /// Turn on this field to load displacement textures.
        /// </summary>
        public bool LoadDisplacementTextures;

        /// <summary>
        /// Turn off this field if your avatars seem to hover over the ground. That will disable avatar hips height compensation.
        /// </summary>
        public bool ApplyAvatarHipsCompensation = true;

        /// <summary>
        /// Use this field to define a transformation applied in world space to all mesh vertices.
        /// </summary>
        public Matrix4x4 MeshWorldTransform = Matrix4x4.identity;

        /// <summary>
        /// Turn on this field to load textures using the UnityWebRequest class (experimental). UnityWebRequest is the fastest way to load PNG/JPG textures but uses more memory than other methods and does not work with embedded textures.
        /// <remarks>
        /// This feature is experimental. Textures are always loaded in sRGB colorspace when this option is turned on, which could cause normal map issues.
        /// </remarks>
        /// </summary>
        public bool LoadTexturesViaWebRequest;

        /// <summary>
        /// Use this field to set the maximum number of objects TriLib can rename. Renaming is an expensive process, so it's advised to keep this value low.
        /// </summary>
        public int MaxObjectsToRename = 1024;

        /// <summary>
        /// Turn off this field to keep processed/composed textures as RenderTextures.
        /// </summary>
        public bool ConvertTexturesAs2D = true;

        /// <summary>
        /// Turn off this field to use the renderers' "materials" property instead of "sharedMaterials".
        /// </summary>
        public bool UseSharedMaterials = true;

        /// <summary>
        /// Turn on this field to use mesh filters' "mesh" property instead of "sharedMesh".
        /// It is recommended to turn on this field if you don't need to modify your meshes at runtime.
        /// </summary>
        public bool UseSharedMeshes = true;

        /// <summary>
        /// Use this field to set a BlendShapeMapper to use with the loaded model. BlendShapeMappers can replace the Unity built-in blend shape playback system.
        /// </summary>
        public BlendShapeMapper BlendShapeMapper;

        /// <summary>
        /// Turn on this field to update SkinnedMeshRenderers when they're offscreen.
        /// </summary>
        public bool UpdateSkinnedMeshRendererWhenOffscreen;

        /// <summary>
        /// Turn off this field to disable Phong to PBR conversion based on:
        /// https://learn.microsoft.com/en-us/azure/remote-rendering/reference/material-mapping
        /// </summary>
        public bool DoPBRConversion = true;

        /// <summary>
        /// Turn off this field to disable composing the Albedo texture using the original material's transparency texture.
        /// </summary>
        public bool ApplyTransparencyTexture = true;
        
        /// <summary>
        /// Turn on this field to bake the model transform into models and animations (Experimental).
        /// </summary>
        public bool BakeAxisConversion;

        /// <summary>
        /// Deserialize the specified JSON representation into this class.
        /// </summary>
        /// <param name="json">Json.</param>
        public void Deserialize(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }

        /// <summary>
        /// Serializes this instance to a JSON representation.
        /// </summary>
        public string Serialize()
        {
            return JsonUtility.ToJson(this);
        }

    }
}
