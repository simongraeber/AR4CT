using UnityEngine;

namespace TriLibCore.Mappers
{
    /// <summary>
    /// Provides a mechanism for handling custom properties embedded within 3D model data.
    /// By subclassing <see cref="UserPropertiesMapper"/>, developers can intercept and process 
    /// user-defined attributes (e.g., metadata or extended properties) attached to a model’s 
    /// nodes, materials, or other elements.
    /// </summary>
    public class UserPropertiesMapper : ScriptableObject
    {
        /// <summary>
        /// Invoked whenever a user-defined property (e.g., custom FBX attributes, metadata fields, etc.) 
        /// is found on a node in the loaded model. Override this method to implement 
        /// custom logic for storing, interpreting, or reacting to these properties in your 
        /// Unity <see cref="GameObject"/> hierarchy.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> containing references to loaded model objects,
        /// import settings, and other contextual data.
        /// </param>
        /// <param name="gameObject">
        /// The <see cref="GameObject"/> to which the property belongs.
        /// </param>
        /// <param name="propertyName">
        /// The name or identifier of the custom property (e.g., "collisionType", "lightmapIndex").
        /// </param>
        /// <param name="propertyValue">
        /// The value of the custom property. Its type can vary (e.g., <see cref="string"/>, 
        /// <see cref="float"/>, arrays, or other complex data structures).
        /// </param>
        public virtual void OnProcessUserData(
            AssetLoaderContext assetLoaderContext,
            GameObject gameObject,
            string propertyName,
            object propertyValue)
        {
        }
    }
}
