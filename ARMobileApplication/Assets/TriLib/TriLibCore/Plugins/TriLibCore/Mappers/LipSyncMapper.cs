using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Mappers
{
    /// <summary>
    /// Provides a mechanism for mapping blend shapes within a model’s geometry to 
    /// corresponding visemes (lip-sync shapes). This class can be extended to integrate 
    /// TriLib-loaded models with custom lip-sync systems, enabling facial animation driven 
    /// by audio or other input sources.
    /// </summary>
    public class LipSyncMapper : ScriptableObject
    {
        /// <summary>
        /// Defines the mapper’s priority when multiple <see cref="LipSyncMapper"/> instances 
        /// are present in the same <see cref="AssetLoaderOptions"/>. A lower value means 
        /// this mapper will be attempted first.
        /// </summary>
        public int CheckingOrder;

        /// <summary>
        /// Specifies the total number of lip-sync visemes recognized by this mapper, 
        /// allowing for indexed lookups or array-based mappings.
        /// </summary>
        public const int VisemeCount = 14;

        /// <summary>
        /// Attempts to match each of the <see cref="VisemeCount"/> visemes 
        /// to corresponding blend shape keys in the provided <paramref name="geometryGroup"/>. 
        /// If any matches are found, their indices are stored in the <paramref name="output"/> array.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> holding model loading data, including 
        /// loaded <see cref="GameObject"/> references, options, and callbacks.
        /// </param>
        /// <param name="geometryGroup">
        /// The <see cref="IGeometryGroup"/> containing blend shape data 
        /// (<see cref="IGeometryGroup.BlendShapeKeys"/>).
        /// </param>
        /// <param name="output">
        /// An integer array of length <see cref="VisemeCount"/> that, upon success, 
        /// will hold the matched blend shape key indices for each viseme. If no match is found 
        /// for a given viseme, the corresponding index is set to <c>-1</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if at least one viseme was successfully mapped to a blend shape key; 
        /// otherwise, <c>false</c>.
        /// </returns>
        public virtual bool Map(
            AssetLoaderContext assetLoaderContext,
            IGeometryGroup geometryGroup,
            out int[] output)
        {
            if (geometryGroup.BlendShapeKeys == null)
            {
                output = null;
                return false;
            }

            var result = false;
            output = new int[VisemeCount];

            // For each known viseme, attempt to find a blend shape match
            for (var i = 0; i < VisemeCount; i++)
            {
                var mapped = MapViseme(assetLoaderContext, (LipSyncViseme)i, geometryGroup);
                if (mapped > -1)
                {
                    output[i] = mapped;
                    result = true;
                }
                else
                {
                    output[i] = -1;
                }
            }
            return result;
        }

        /// <summary>
        /// Maps a single viseme (lip-sync shape) to its corresponding blend shape key 
        /// within the specified <paramref name="geometryGroup"/>, returning the index of 
        /// that blend shape key if found. 
        /// Override this method to customize name matching or heuristic-based matching.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> referencing the model’s loaded data 
        /// and import settings.
        /// </param>
        /// <param name="viseme">
        /// The specific lip-sync shape to match.
        /// </param>
        /// <param name="geometryGroup">
        /// The geometric data group holding blend shape keys. This method tries to 
        /// locate one that corresponds to the requested viseme.
        /// </param>
        /// <returns>
        /// The blend shape key index if a matching shape is found; otherwise <c>-1</c> 
        /// to indicate no match.
        /// </returns>
        protected virtual int MapViseme(
            AssetLoaderContext assetLoaderContext,
            LipSyncViseme viseme,
            IGeometryGroup geometryGroup)
        {
            return -1;
        }
    }
}
