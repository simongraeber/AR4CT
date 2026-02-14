namespace TriLibCore.Interfaces
{
    /// <summary>Represents a TriLib Object (Base interface used in many TriLib classes).</summary>
    public interface IObject
    {
        /// <summary>Gets/Sets the Object name.</summary>

        string Name { get; set; }

        /// <summary>Gets/Sets the flag indicating whether this object used somewhere.</summary>
        bool Used { get; set; }
    }
}