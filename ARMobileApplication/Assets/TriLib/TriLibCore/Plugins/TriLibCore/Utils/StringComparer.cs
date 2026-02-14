using System;
using System.Globalization;
using TriLibCore.General;

namespace TriLibCore.Utils
{
    /// <summary>Represents a class used to match Strings using various parameters.</summary>
    public static class StringComparer
    {
        /// <summary>Compares two Strings using the class options.</summary>
        /// <param name="stringComparisonMode">The type of comparison to use.</param>
        /// <param name="caseInsensitive">Pass <c>true</c> to do a case-insensitive search.</param>
        /// <param name="left">The left String to compare.</param>
        /// <param name="right">The right String to compare.</param>
        /// <returns>
        /// <c>true</c> if the strings match, <c>false</c> otherwise.</returns>
        public static bool Matches(StringComparisonMode stringComparisonMode, bool caseInsensitive, string left, string right)
        {
            bool matches;
            switch (stringComparisonMode)
            {
                case StringComparisonMode.RightEqualsLeft:
                    matches = right.Equals(left, caseInsensitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
                    break;
                case StringComparisonMode.RightStartsWithLeft:
                    matches = right.StartsWith(left, caseInsensitive, CultureInfo.InvariantCulture);
                    break;
                case StringComparisonMode.RightEndsWithLeft:
                    matches = right.EndsWith(left, caseInsensitive, CultureInfo.InvariantCulture);
                    break;
                case StringComparisonMode.LeftStartsWithRight:
                    matches = left.StartsWith(right, caseInsensitive, CultureInfo.InvariantCulture);
                    break;
                case StringComparisonMode.LeftContainsRight:
                    matches = left.IndexOf(right, caseInsensitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture) >= 0;
                    break;
                case StringComparisonMode.RightContainsLeft:
                    matches = right.IndexOf(left, caseInsensitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture) >= 0;
                    break;
                default:
                    matches = left.EndsWith(right, caseInsensitive, CultureInfo.InvariantCulture);
                    break;
            }
            return matches;
        }
    }
}