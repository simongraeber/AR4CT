namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides utility methods for working with integer-based flags.
    /// </summary>
    public static class FlagUtils
    {
        /// <summary>
        /// Determines whether the specified flag is set within the given integer value.
        /// </summary>
        /// <param name="value">The integer value to check.</param>
        /// <param name="flag">The flag to look for.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="flag"/> is set in <paramref name="value"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool HasFlag(int value, int flag)
        {
            return (value & flag) == flag;
        }
    }
}
