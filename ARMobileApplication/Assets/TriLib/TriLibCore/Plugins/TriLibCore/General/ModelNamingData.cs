namespace TriLibCore
{
    /// <summary>
    /// Holds identifying information for a particular model, including names, part numbers, and class designations.
    /// This data is typically used when generating or customizing names for model-based objects.
    /// </summary>
    public struct ModelNamingData
    {
        /// <summary>
        /// The name assigned to the model.
        /// </summary>
        public string ModelName;

        /// <summary>
        /// The name of the material associated with this model, if any.
        /// </summary>
        public string MaterialName;

        /// <summary>
        /// The part number for this model, if specified.
        /// </summary>
        public string PartNumber;

        /// <summary>
        /// A general-purpose identifier for the model.
        /// </summary>
        public string Id;

        /// <summary>
        /// A class or category designation for the model.
        /// </summary>
        public string Class;
    }
}
