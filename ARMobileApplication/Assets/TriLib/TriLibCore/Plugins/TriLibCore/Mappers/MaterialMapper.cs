#pragma warning disable 618
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TriLibCore.General;
using TriLibCore.Interfaces;
using TriLibCore.Textures;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace TriLibCore.Mappers
{
    /// <summary>
    /// Serves as an abstract base class for converting TriLib virtual materials into Unity
    /// <see cref="Material"/> objects. Classes inheriting from <see cref="MaterialMapper"/> 
    /// must implement or override the provided methods and properties to handle specific 
    /// pipeline requirements (e.g., Standard, HDRP, URP, custom shaders, etc.).
    /// </summary>
    public abstract class MaterialMapper : ScriptableObject
    {
        [Obsolete("Not used on latest version")]
        private static readonly List<string> _registeredMapperNamespaces = new List<string> { };

        [Obsolete("Not used on latest version")]
        private static readonly List<string> _registeredMappers = new List<string> { };

        /// <summary>
        /// If overridden to return <c>true</c>, indicates that this mapper uses a Shader Variant
        /// Collection instead of the usual material presets. By default, it returns <c>false</c>.
        /// </summary>
        /// <remarks>
        /// Implementers can override this to provide advanced material handling, especially when
        /// working with different pipelines or specialized shader setups.
        /// </remarks>
        public virtual bool UseShaderVariantCollection { get; }

        /// <summary>
        /// A numeric priority determining the order in which this mapper is checked for compatibility 
        /// against a given material. Higher values mean earlier checks; mappers with lower priority 
        /// values are evaluated later if no suitable match is found.
        /// </summary>
        public int CheckingOrder;

        /// <summary>
        /// If enabled, alpha-based material features (e.g., transparency) will be disabled, 
        /// effectively forcing the usage of a purely opaque workflow.
        /// </summary>
        [FormerlySerializedAs("ForceStandardMaterial")]
        public bool DisableAlpha;

        /// <summary>
        /// A list of default namespaces used to register TriLib material mappers
        /// (e.g., "TriLibCore.HDRP.Mappers", "TriLibCore.URP.Mappers").
        /// </summary>
        public static List<string> RegisteredMapperNamespaces => _registeredMapperNamespaces;

        /// <summary>
        /// A list of default mapper class names recognized by TriLib 
        /// (e.g., "HDRPMaterialMapper", "UniversalRPMaterialMapper", "StandardMaterialMapper").
        /// </summary>
        public static List<string> RegisteredMappers => _registeredMappers;

        /// <summary>
        /// Indicates whether this Material Mapper automatically extracts Metallic and Smoothness textures.
        /// </summary>
        public virtual bool ExtractMetallicAndSmoothness { get; } = true;

        #region Standard Presets
        /// <summary>
        /// A cutout material preset for materials that use alpha testing 
        /// (fully transparent vs. fully opaque).
        /// </summary>
        public virtual Material CutoutMaterialPreset { get; }

        /// <summary>
        /// A cutout material preset for materials with no metallic texture usage.
        /// </summary>
        public virtual Material CutoutMaterialPresetNoMetallicTexture { get; }

        /// <summary>
        /// The default (opaque) material preset.
        /// </summary>
        public virtual Material MaterialPreset { get; }

        /// <summary>
        /// The default (opaque) material preset for materials with no metallic texture usage.
        /// </summary>
        public virtual Material MaterialPresetNoMetallicTexture { get; }

        /// <summary>
        /// A “compose” material preset used in layered alpha workflows when 
        /// <see cref="AssetLoaderOptions.AlphaMaterialMode"/> is set to <c>CutoutAndTransparent</c>.
        /// This is applied as a secondary pass for partially transparent regions.
        /// </summary>
        public virtual Material TransparentComposeMaterialPreset { get; }

        /// <summary>
        /// The “compose” material preset for partially transparent materials 
        /// that have no metallic texture usage.
        /// </summary>
        public virtual Material TransparentComposeMaterialPresetNoMetallicTexture { get; }

        /// <summary>
        /// A fully transparent (alpha) material preset.
        /// </summary>
        public virtual Material TransparentMaterialPreset { get; }

        /// <summary>
        /// A fully transparent (alpha) material preset for materials without a metallic texture.
        /// </summary>
        public virtual Material TransparentMaterialPresetNoMetallicTexture { get; }
        #endregion

        /// <summary>
        /// An optional placeholder material used while a model’s final materials and textures
        /// are still loading.
        /// </summary>
        public virtual Material LoadingMaterial => null;

        /// <summary>
        /// Indicates whether this mapper’s <see cref="Map"/> process requires an 
        /// asynchronous coroutine approach (<c>true</c>), or can run synchronously (<c>false</c>).
        /// </summary>
        /// <remarks>
        /// Returning <c>true</c> means <see cref="MapCoroutine"/> will be called instead of <see cref="Map"/>.
        /// </remarks>
        public virtual bool UsesCoroutines => false;

        /// <summary>
        /// Indicates whether this Material Mapper does "Metallic/Smoothness/Specular/Roughness/Emission" automatic texture creation.
        /// </summary>
        public virtual bool ConvertMaterialTextures => false;

        /// <summary>
        /// Applies the final Unity <see cref="Material"/> to the given <see cref="Renderer"/> 
        /// based on the <paramref name="materialRendererContext"/>. If 
        /// <see cref="AssetLoaderOptions.AlphaMaterialMode"/> is set to 
        /// <c>CutoutAndTransparent</c>, a second pass material may be created for partial translucency.
        /// </summary>
        /// <param name="materialRendererContext">
        /// The context containing a <see cref="Renderer"/>, geometry data, and references to 
        /// the TriLib <see cref="MaterialMapperContext"/>.
        /// </param>
        public void ApplyMaterialToRenderer(MaterialRendererContext materialRendererContext)
        {
            var materialMapperContext = materialRendererContext.MaterialMapperContext;
            if (materialRendererContext.Context.Options.UseSharedMaterials)
            {
                var sharedMaterials = materialRendererContext.Renderer.sharedMaterials;
                sharedMaterials[materialRendererContext.GeometryIndex] = materialMapperContext.UnityMaterial;
                materialRendererContext.Renderer.sharedMaterials = sharedMaterials;
            }
            else
            {
                var sharedMaterials = materialRendererContext.Renderer.materials;
                sharedMaterials[materialRendererContext.GeometryIndex] = materialMapperContext.UnityMaterial;
                materialRendererContext.Renderer.materials = sharedMaterials;
            }
            var applyAlpha =
                !materialRendererContext.MaterialMapperContext.MaterialMapper.UseShaderVariantCollection &&
                materialMapperContext.Context.Options.AlphaMaterialMode == AlphaMaterialMode.CutoutAndTransparent &&
                materialMapperContext.VirtualMaterial.HasAlpha &&
                !DisableAlpha;
            if (applyAlpha && materialRendererContext.Mesh != null)
            {
                var materials = new List<Material>();
                materialRendererContext.Renderer.GetSharedMaterials(materials);
                var triangles = materialRendererContext.Mesh.GetTriangles(materialRendererContext.GeometryIndex);
                var secondMaterial = InstantiateSuitableSecondPassMaterial(materialMapperContext);
                if (secondMaterial == null)
                {
                    return;
                }
                materialMapperContext.AlphaMaterial = secondMaterial;
                materialRendererContext.Mesh.subMeshCount++;
                materialRendererContext.Mesh.SetTriangles(triangles, materialRendererContext.Mesh.subMeshCount - 1);
                materials.Add(secondMaterial);
                if (materialRendererContext.Context.Options.UseSharedMaterials)
                {
                    materialRendererContext.Renderer.sharedMaterials = materials.ToArray();
                }
                else
                {
                    materialRendererContext.Renderer.materials = materials.ToArray();
                }
            }
        }

        /// <summary>
        /// Releases CPU mesh data (if allowed) once the material has been assigned,
        /// and if no blend shape mapper is in use. This can reduce runtime memory usage.
        /// </summary>
        /// <param name="materialRendererContext">
        /// The context for a particular <see cref="Renderer"/> and submesh, containing the model’s mesh reference.
        /// </param>
        public static void Cleanup(MaterialRendererContext materialRendererContext)
        {
            if (materialRendererContext.Mesh != null)
            {
                materialRendererContext.Mesh.UploadMeshData(
                    !materialRendererContext.Context.Options.ReadEnabled &&
                    materialRendererContext.Context.Options.BlendShapeMapper == null
                );
            }
        }

        /// <summary>
        /// Retrieves the property name for the diffuse (albedo) color within this mapper’s target shaders.
        /// </summary>
        /// <param name="materialMapperContext">Context containing the TriLib virtual material and Unity material references.</param>
        public virtual string GetDiffuseColorName(MaterialMapperContext materialMapperContext) => null;

        /// <summary>
        /// Retrieves the property name for the diffuse (albedo) texture within this mapper’s target shaders.
        /// </summary>
        /// <param name="materialMapperContext">Context containing the TriLib virtual material and Unity material references.</param>
        public virtual string GetDiffuseTextureName(MaterialMapperContext materialMapperContext) => null;

        /// <summary>
        /// Retrieves the property name for the emissive color within this mapper’s target shaders.
        /// </summary>
        /// <param name="materialMapperContext">Context containing the TriLib virtual material and Unity material references.</param>
        public virtual string GetEmissionColorName(MaterialMapperContext materialMapperContext) => null;

        /// <summary>
        /// Retrieves the property name for glossiness or roughness, depending on the shader’s workflow.
        /// </summary>
        /// <param name="materialMapperContext">Context containing the TriLib virtual material and Unity material references.</param>
        public virtual string GetGlossinessOrRoughnessName(MaterialMapperContext materialMapperContext) => null;

        /// <summary>
        /// Retrieves the property name for the glossiness or roughness texture, if applicable.
        /// </summary>
        /// <param name="materialMapperContext">Context containing the TriLib virtual material and Unity material references.</param>
        public virtual string GetGlossinessOrRoughnessTextureName(MaterialMapperContext materialMapperContext) => null;

        /// <summary>
        /// Retrieves the property name for metallic values in PBR workflows.
        /// </summary>
        /// <param name="materialMapperContext">Context containing the TriLib virtual material and Unity material references.</param>
        public virtual string GetMetallicName(MaterialMapperContext materialMapperContext) => null;

        /// <summary>
        /// Retrieves the property name for the metallic texture, if used in the target shader.
        /// </summary>
        /// <param name="materialMapperContext">Context containing the TriLib virtual material and Unity material references.</param>
        public virtual string GetMetallicTextureName(MaterialMapperContext materialMapperContext) => null;

        /// <summary>
        /// Determines whether this mapper can handle the specified material (e.g., by checking
        /// shader keywords, pipeline features, or other criteria). Mappers with <c>false</c> returns 
        /// are skipped in favor of others with higher compatibility.
        /// </summary>
        /// <param name="materialMapperContext">
        /// The context providing references to both the TriLib <c>VirtualMaterial</c> data
        /// and the underlying Unity <see cref="Material"/>.
        /// </param>
        /// <returns><c>true</c> if compatible; otherwise <c>false</c>.</returns>
        public virtual bool IsCompatible(MaterialMapperContext materialMapperContext)
        {
            return false;
        }

        /// <summary>
        /// Begins the material mapping process synchronously, converting TriLib’s 
        /// <see cref="VirtualMaterial"/> into a Unity <see cref="Material"/> (or multiple passes).
        /// </summary>
        /// <param name="materialMapperContext">
        /// Holds references to the TriLib virtual material, the target Unity material, 
        /// and the overall <see cref="AssetLoaderContext"/>.
        /// </param>
        public virtual void Map(MaterialMapperContext materialMapperContext)
        {
        }

        /// <summary>
        /// Begins the material mapping process asynchronously, yielding control back to the caller
        /// to allow frame updates or concurrent loading. Called if <see cref="UsesCoroutines"/> 
        /// returns <c>true</c>.
        /// </summary>
        /// <param name="materialMapperContext">
        /// Holds references to the TriLib virtual material, the target Unity material, 
        /// and the overall <see cref="AssetLoaderContext"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> sequence that yields at intermediate steps for cooperative 
        /// asynchronous processing.
        /// </returns>
        public virtual IEnumerable MapCoroutine(MaterialMapperContext materialMapperContext)
        {
            yield break;
        }

        /// <summary>
        /// Ensures that the loaded texture respects any offset or scaling set in the TriLib
        /// virtual material. This method updates either the Unity <see cref="Material"/> 
        /// or the <see cref="VirtualMaterial"/> properties depending on the setup.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// Contains texture data, references to the <see cref="MaterialMapperContext"/>, and 
        /// flags indicating if the texture was successfully loaded.
        /// </param>
        protected static void CheckTextureOffsetAndScaling(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.TextureLoaded &&
                textureLoadingContext.Texture != null &&
                textureLoadingContext.MaterialMapperContext.Context.Options.ApplyTexturesOffsetAndScaling)
            {
                if (!textureLoadingContext.MaterialMapperContext.Material.ApplyOffsetAndScale(textureLoadingContext))
                {
                    textureLoadingContext.MaterialMapperContext.VirtualMaterial.Tiling = textureLoadingContext.Texture.Tiling;
                    textureLoadingContext.MaterialMapperContext.VirtualMaterial.Offset = textureLoadingContext.Texture.Offset;
                }
            }
        }

        /// <summary>
        /// (Obsolete) An older version of <see cref="CheckTextureOffsetAndScaling"/> that 
        /// takes separate parameters for the TriLib texture and a loaded flag. 
        /// Please use the newer single-parameter method instead.
        /// </summary>
        /// <param name="materialMapperContext">
        /// Contains references to the TriLib virtual material and the Unity material being generated.
        /// </param>
        /// <param name="texture">The TriLib <see cref="ITexture"/> being applied.</param>
        /// <param name="textureLoaded">Whether the texture has successfully loaded into memory.</param>
        [Obsolete("Please use the new method (with a single parameter).")]
        protected static void CheckTextureOffsetAndScaling(MaterialMapperContext materialMapperContext, ITexture texture, bool textureLoaded)
        {
            if (textureLoaded && texture != null && materialMapperContext.Context.Options.ApplyTexturesOffsetAndScaling)
            {
                materialMapperContext.VirtualMaterial.Tiling = texture.Tiling;
                materialMapperContext.VirtualMaterial.Offset = texture.Offset;
            }
        }

        /// <summary>
        /// A coroutine-based version of <see cref="CheckTextureOffsetAndScaling"/>, useful for
        /// stepped or asynchronous processing in complex loading workflows.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// Contains the texture data and references to the <see cref="MaterialMapperContext"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> that yields immediately after applying offset and scale 
        /// (for consistency with async logic).
        /// </returns>
        protected static IEnumerable CheckTextureOffsetAndScalingCoroutine(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.TextureLoaded &&
                textureLoadingContext.Texture != null &&
                textureLoadingContext.MaterialMapperContext.Context.Options.ApplyTexturesOffsetAndScaling)
            {
                if (!textureLoadingContext.MaterialMapperContext.Material.ApplyOffsetAndScale(textureLoadingContext))
                {
                    textureLoadingContext.MaterialMapperContext.VirtualMaterial.Tiling = textureLoadingContext.Texture.Tiling;
                    textureLoadingContext.MaterialMapperContext.VirtualMaterial.Offset = textureLoadingContext.Texture.Offset;
                }
            }
            yield break;
        }

        /// <summary>
        /// Composes a final Unity material based on the TriLib <see cref="VirtualMaterial"/> properties
        /// and the specific property names or overrides provided by an inheriting mapper.
        /// </summary>
        /// <param name="materialMapperContext">
        /// References the TriLib virtual material, the in-progress Unity material, 
        /// and the overall <see cref="AssetLoaderContext"/>.
        /// </param>
        protected void BuildMaterial(MaterialMapperContext materialMapperContext)
        {
            if (materialMapperContext.VirtualMaterial == null)
            {
                return;
            }
            var diffuseColorName = GetDiffuseColorName(materialMapperContext);
            if (!materialMapperContext.Material.MixAlbedoColorWithTexture &&
                materialMapperContext.VirtualMaterial.GenericPropertyIsSetAndValid(GenericMaterialProperty.DiffuseColor) &&
                diffuseColorName != null)
            {
                materialMapperContext.VirtualMaterial.SetProperty(diffuseColorName, Color.white);
            }
            var emissionColorName = GetEmissionColorName(materialMapperContext);
            if (!materialMapperContext.VirtualMaterial.GenericColorPropertyIsSetAndValid(GenericMaterialProperty.EmissionColor) &&
                materialMapperContext.VirtualMaterial.GenericPropertyIsSetAndValid(GenericMaterialProperty.EmissionColor) &&
                emissionColorName != null)
            {
                materialMapperContext.VirtualMaterial.SetProperty(emissionColorName, Color.white);
            }
            if (materialMapperContext.MaterialMapper.ExtractMetallicAndSmoothness && materialMapperContext.Context.Options.ConvertMaterialTextures)
            {
                if (materialMapperContext.VirtualMaterial.GenericColorPropertyIsSetAndValid(GenericMaterialProperty.EmissionColor) &&
                    !materialMapperContext.VirtualMaterial.GenericPropertyIsSetAndValid(GenericMaterialProperty.EmissionColor) &&
                    emissionColorName != null
                )
                {
                    materialMapperContext.VirtualMaterial.SetProperty(emissionColorName, DefaultTextures.White);
                }
            }
            var glossinessOrRoughnessName = GetGlossinessOrRoughnessName(materialMapperContext);
            if (materialMapperContext.VirtualMaterial.GenericPropertyIsSetAndValid(GenericMaterialProperty.GlossinessOrRoughness) &&
                glossinessOrRoughnessName != null &&
                materialMapperContext.Context.Options.DoPBRConversion)
            {
                materialMapperContext.VirtualMaterial.SetProperty(glossinessOrRoughnessName, 1f);
            }
            var metallicName = GetMetallicName(materialMapperContext);
            if (materialMapperContext.VirtualMaterial.GenericPropertyIsSetAndValid(GenericMaterialProperty.Metallic) &&
                metallicName != null &&
                materialMapperContext.Context.Options.DoPBRConversion)
            {
                materialMapperContext.VirtualMaterial.SetProperty(metallicName, 1f);
            }

            // Create an actual Unity material preset instance
            materialMapperContext.UnityMaterial = InstantiateSuitableMaterial(materialMapperContext);

            // Set offset and tiling if any
            foreach (var texture in materialMapperContext.VirtualMaterial.TextureProperties.Keys)
            {
                if (materialMapperContext.UnityMaterial.HasProperty(texture))
                {
                    materialMapperContext.UnityMaterial.SetTextureOffset(texture, materialMapperContext.VirtualMaterial.Offset);
                    materialMapperContext.UnityMaterial.SetTextureScale(texture, materialMapperContext.VirtualMaterial.Tiling);
                }
            }
        }

        /// <summary>
        /// Loads a texture synchronously while providing optional callback actions that are triggered 
        /// once the texture is fully processed.
        /// </summary>
        /// <param name="materialMapperContext">
        /// Provides references to the TriLib virtual material, the Unity material, 
        /// and the broader <see cref="AssetLoaderContext"/>.
        /// </param>
        /// <param name="textureType">Indicates the conceptual role of the texture (e.g., diffuse, normal, metallic).</param>
        /// <param name="texture">The TriLib <see cref="ITexture"/> to be loaded.</param>
        /// <param name="onTextureProcessed">
        /// Zero or more callback actions invoked after the texture is processed. Each action
        /// receives a <see cref="TextureLoadingContext"/> with details about the load result.
        /// </param>
        protected void LoadTextureWithCallbacks(MaterialMapperContext materialMapperContext, TextureType textureType, ITexture texture, params Action<TextureLoadingContext>[] onTextureProcessed)
        {
            var onTextureProcessedEnumerator = new Func<TextureLoadingContext, IEnumerator>[onTextureProcessed.Length];
            for (var i = 0; i < onTextureProcessedEnumerator.Length; i++)
            {
                var index = i;
                onTextureProcessedEnumerator[index] = delegate (TextureLoadingContext textureLoadingContext)
                {
                    onTextureProcessed[index](textureLoadingContext);
                    return null;
                };
            }
            foreach (var item in LoadTextureWithCoroutineCallbacks(materialMapperContext, textureType, texture))
            {
                // Synchronously consume any yielded items from the coroutine approach
            }
        }

        /// <summary>
        /// Loads a texture asynchronously, returning a coroutine-like sequence of steps 
        /// which can be interleaved with other tasks. This is useful for large textures 
        /// or slower I/O operations.
        /// </summary>
        /// <param name="materialMapperContext">
        /// Context referencing the TriLib virtual material, the target Unity material, and
        /// the broader <see cref="AssetLoaderContext"/>.
        /// </param>
        /// <param name="textureType">Indicates the conceptual role of the texture (e.g., diffuse, normal, metallic).</param>
        /// <param name="texture">The TriLib <see cref="ITexture"/> to be loaded.</param>
        /// <param name="onTextureProcessed">
        /// Zero or more callback functions (coroutines) executed after texture loading 
        /// to apply post-processing or run custom logic (e.g., compressing, adjusting channels, etc.).
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable"/> enumerator that yields at various stages of texture loading
        /// for cooperative multitasking.
        /// </returns>
        protected IEnumerable LoadTextureWithCoroutineCallbacks(MaterialMapperContext materialMapperContext, TextureType textureType, ITexture texture, params Func<TextureLoadingContext, IEnumerable>[] onTextureProcessed)
        {
            var textureLoadingContext = new TextureLoadingContext()
            {
                TextureType = textureType,
                Texture = texture,
                MaterialMapperContext = materialMapperContext,
                Context = materialMapperContext.Context,
            };

            // Begin the actual texture load if the texture is provided
            if (textureLoadingContext.Texture != null)
            {
                textureLoadingContext.MaterialMapperContext.Context.TryRegisterTexture(textureLoadingContext);

                // If the user wants to create textures in memory, do so before final loading
                if (!textureLoadingContext.TextureLoaded &&
                    !textureLoadingContext.Context.Options.LoadTexturesAtOnce &&
                    !textureLoadingContext.Context.Options.UseUnityNativeTextureLoader)
                {
                    TextureLoaders.CreateTexture(textureLoadingContext);
                }

                // If the user wants to load textures via UnityWebRequest (instead of local or embedded)
                if (!textureLoadingContext.TextureLoaded &&
                    textureLoadingContext.Context.Options.LoadTexturesViaWebRequest &&
                    textureLoadingContext.Texture.ResolvedFilename != null)
                {
                    textureLoadingContext.Stream = File.OpenRead(textureLoadingContext.Texture.ResolvedFilename);
                    var unityWebRequest = UnityWebRequestTexture.GetTexture($"file://{textureLoadingContext.Texture.ResolvedFilename.Replace("\\", "/")}");
                    yield return unityWebRequest.SendWebRequest();
                    if (string.IsNullOrWhiteSpace(unityWebRequest.error))
                    {
                        var downloadHandlerTexture = (DownloadHandlerTexture)unityWebRequest.downloadHandler;
                        textureLoadingContext.OriginalUnityTexture = textureLoadingContext.UnityTexture = downloadHandlerTexture.texture;
                        textureLoadingContext.Width = textureLoadingContext.UnityTexture.width;
                        textureLoadingContext.Height = textureLoadingContext.UnityTexture.height;
                        textureLoadingContext.TextureLoaded = true;
                    }
                }

                // Attempt to load via the main TriLib texture loading pipeline
                if (!textureLoadingContext.TextureLoaded && TextureLoaders.LoadTexture(textureLoadingContext))
                {
                    foreach (var item in materialMapperContext.Context.ReleaseMainThread())
                    {
                        yield return item;
                    }
                }

                // If the texture is loaded, do alpha scanning, NPOT fixes, normal map fixes, etc.
                if (textureLoadingContext.TextureLoaded)
                {
                    // Check for alpha pixels
                    if (textureLoadingContext.Context.Options.ScanForAlphaPixels)
                    {
                        TextureLoaders.ScanForAlphaPixels(textureLoadingContext);
                        foreach (var item in materialMapperContext.Context.ReleaseMainThread())
                        {
                            yield return item;
                        }
                    }
                    // If not using native loader, apply it to the Unity material
                    if (!textureLoadingContext.Context.Options.UseUnityNativeTextureLoader)
                    {
                        TextureLoaders.ApplyTexture(textureLoadingContext, false);
                        foreach (var item in materialMapperContext.Context.ReleaseMainThread())
                        {
                            yield return item;
                        }
                    }
                    // Enforce power-of-two if requested
                    if (textureLoadingContext.Context.Options.ForcePowerOfTwoTextures)
                    {
                        TextureLoaders.FixNPOTTexture(textureLoadingContext);
                        foreach (var item in materialMapperContext.Context.ReleaseMainThread())
                        {
                            yield return item;
                        }
                    }
                    // Fix normal map channels if requested
                    if (textureLoadingContext.Context.Options.FixNormalMaps)
                    {
                        TextureLoaders.FixNormalMap(textureLoadingContext);
                        foreach (var item in materialMapperContext.Context.ReleaseMainThread())
                        {
                            yield return item;
                        }
                    }
                }
            }

            // Perform final texture post-processing
            if (TextureLoaders.PostProcessTexture(textureLoadingContext))
            {
                foreach (var item in materialMapperContext.Context.ReleaseMainThread())
                {
                    yield return item;
                }
            }

            // Execute each callback in onTextureProcessed as a mini-coroutine
            foreach (var callback in onTextureProcessed)
            {
                var result = callback(textureLoadingContext);
                foreach (var x in result)
                {
                    foreach (var item in materialMapperContext.Context.ReleaseMainThread())
                    {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Applies stored property values (colors, floats, vectors, textures, etc.) from
        /// the TriLib <see cref="VirtualMaterial"/> into the final Unity <see cref="Material"/>.
        /// </summary>
        /// <param name="materialMapperContext">Contains references to both TriLib and Unity material objects.</param>
        /// <param name="finalMaterial">The instantiated Unity material to be configured.</param>
        private static void ApplyMaterialProperties(MaterialMapperContext materialMapperContext, Material finalMaterial)
        {
            finalMaterial.name = materialMapperContext.Material.Name;
            foreach (var kvp in materialMapperContext.VirtualMaterial.FloatProperties)
            {
                finalMaterial.SetFloat(kvp.Key, kvp.Value);
            }
            foreach (var kvp in materialMapperContext.VirtualMaterial.VectorProperties)
            {
                finalMaterial.SetVector(kvp.Key, kvp.Value);
            }
            foreach (var kvp in materialMapperContext.VirtualMaterial.ColorProperties)
            {
                finalMaterial.SetVector(kvp.Key, kvp.Value);
            }
            foreach (var kvp in materialMapperContext.VirtualMaterial.TextureProperties)
            {
                if (!materialMapperContext.Context.Options.SetUnusedTexturePropertiesToNull && kvp.Value == null)
                {
                    continue;
                }
                finalMaterial.SetTexture(kvp.Key, kvp.Value);
            }

            // If the user wants TriLib to set keyword states, do so
            if (materialMapperContext.Context.Options.UseMaterialKeywords ||
                materialMapperContext.MaterialMapper.UseShaderVariantCollection)
            {
                foreach (var kvp in materialMapperContext.VirtualMaterial.Keywords)
                {
                    if (kvp.Value)
                    {
                        finalMaterial.EnableKeyword(kvp.Key);
                    }
                    else
                    {
                        finalMaterial.DisableKeyword(kvp.Key);
                    }
                }
                finalMaterial.globalIlluminationFlags = materialMapperContext.VirtualMaterial.GlobalIlluminationFlags;
            }
        }

        /// <summary>
        /// Checks if the <see cref="VirtualMaterial"/> contains references to metallic, roughness, or smoothness textures.
        /// </summary>
        /// <param name="materialMapperContext">Provides references to TriLib’s virtual material and the Unity <see cref="Material"/>.</param>
        /// <returns><c>true</c> if any relevant texture is found; otherwise <c>false</c>.</returns>
        private static bool HasMetallicRoughnessOrSmoothnessTexture(MaterialMapperContext materialMapperContext)
        {
            return materialMapperContext.VirtualMaterial != null && (materialMapperContext.VirtualMaterial.GenericPropertyIsSetAndValid(GenericMaterialProperty.MetallicMap) || materialMapperContext.VirtualMaterial.GenericPropertyIsSetAndValid(GenericMaterialProperty.GlossinessOrRoughnessMap));
        }

        /// <summary>
        /// Instantiates an appropriate Unity material preset based on alpha usage, presence of metallic textures,
        /// and mapper-specific material references. Falls back to a basic “Standard” shader if no presets
        /// are available.
        /// </summary>
        /// <param name="materialMapperContext">The context storing references to the TriLib and Unity material data.</param>
        /// <returns>An instantiated Unity material ready for property assignment.</returns>
        private Material InstantiateSuitableMaterial(MaterialMapperContext materialMapperContext)
        {
            Material materialPreset;
            var applyAlpha =
                materialMapperContext.Context.Options.AlphaMaterialMode != AlphaMaterialMode.None &&
                materialMapperContext.VirtualMaterial.HasAlpha &&
                !DisableAlpha;
            var hasMetallic = HasMetallicRoughnessOrSmoothnessTexture(materialMapperContext);

            if (applyAlpha)
            {
                switch (materialMapperContext.Context.Options.AlphaMaterialMode)
                {
                    case AlphaMaterialMode.Transparent:
                        materialPreset = hasMetallic
                            ? TransparentMaterialPreset
                            : TransparentMaterialPresetNoMetallicTexture
                                ? TransparentMaterialPresetNoMetallicTexture
                                : TransparentMaterialPreset;
                        break;
                    default: // alpha cutout
                        materialPreset = hasMetallic
                            ? CutoutMaterialPreset
                            : CutoutMaterialPresetNoMetallicTexture
                                ? CutoutMaterialPresetNoMetallicTexture
                                : CutoutMaterialPreset;
                        break;
                }
            }
            else
            {
                materialPreset = hasMetallic
                    ? MaterialPreset
                    : MaterialPresetNoMetallicTexture
                        ? MaterialPresetNoMetallicTexture
                        : MaterialPreset;
            }

            // Fallback to Unity’s built-in “Standard” if no valid preset is found
            if (materialPreset == null)
            {
                materialPreset = new Material(Shader.Find("Standard"));
                if (materialMapperContext.Context.Options.ShowLoadingWarnings)
                {
                    Debug.LogError("TriLib was unable to find a suitable material preset.");
                }
            }

            var material = Instantiate(materialPreset);
            if (material != null)
            {
                ApplyMaterialProperties(materialMapperContext, material);
                materialMapperContext.Context.LoadedMaterials[materialMapperContext.Material] = material;
                materialMapperContext.Context.Allocations.Add(material);
                if (materialMapperContext.Context.Options.ShowLoadingWarnings)
                {
                    Debug.Log($"Created material [{material.name}]");
                }
            }
            return material;
        }

        /// <summary>
        /// Creates or retrieves a second-pass material for partial alpha effects 
        /// when using <c>AlphaMaterialMode.CutoutAndTransparent</c>. This additional
        /// material is used to render partially translucent areas layered over the 
        /// original submesh pass.
        /// </summary>
        /// <param name="materialMapperContext">
        /// Holds references to the virtual and Unity materials, as well as the 
        /// <see cref="AssetLoaderContext"/>.
        /// </param>
        /// <returns>
        /// A new Unity <see cref="Material"/> instance or an existing cached alpha material,
        /// or <c>null</c> if no suitable material could be instantiated.
        /// </returns>
        private Material InstantiateSuitableSecondPassMaterial(MaterialMapperContext materialMapperContext)
        {
            if (!materialMapperContext.Context.GeneratedMaterials.TryGetValue(materialMapperContext.Material, out var material))
            {
                Material materialPreset;
                var hasMetallic = HasMetallicRoughnessOrSmoothnessTexture(materialMapperContext);

                materialPreset = hasMetallic
                    ? TransparentComposeMaterialPreset
                    : TransparentComposeMaterialPresetNoMetallicTexture
                        ? TransparentComposeMaterialPresetNoMetallicTexture
                        : TransparentComposeMaterialPreset;

                if (materialPreset == null)
                {
                    // If no second-pass alpha preset is found, fallback to the standard presets
                    materialPreset = hasMetallic
                        ? MaterialPreset
                        : MaterialPresetNoMetallicTexture
                            ? MaterialPresetNoMetallicTexture
                            : MaterialPreset;
                }

                if (materialPreset == null)
                {
                    materialPreset = new Material(Shader.Find("Standard"));
                    if (materialMapperContext.Context.Options.ShowLoadingWarnings)
                    {
                        Debug.LogError("TriLib was unable to find a suitable material preset for second-pass alpha.");
                    }
                }

                material = Instantiate(materialPreset);
                ApplyMaterialProperties(materialMapperContext, material);
                material.name = $"{material.name}_alpha";
                materialMapperContext.Context.GeneratedMaterials.Add(materialMapperContext.Material, material);
                materialMapperContext.Context.Allocations.Add(material);
            }
            return material;
        }
    }
}
