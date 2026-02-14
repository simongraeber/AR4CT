namespace TriLibCore.General
{
    /// <summary>Represents String matching methods.</summary>
    public enum StringComparisonMode
    {
        /// <summary>Match is valid when Left String ends with Right String</summary>
        LeftEndsWithRight,
        /// <summary>Match is valid when Left String starts with Right String</summary>
        LeftStartsWithRight,
        /// <summary>Match is valid when Right String ends with Left String.</summary>
        RightEndsWithLeft,
        /// <summary>Match is valid when Right String starts with Left String.</summary>
        RightStartsWithLeft,
        /// <summary>Match is valid when both Strings are equal.</summary>
        RightEqualsLeft,
        /// <summary>Match is valid Left String contains the Right String.</summary>
        LeftContainsRight,
        /// <summary>Match is valid Right String contains the Left String.</summary>
        RightContainsLeft
    }
}