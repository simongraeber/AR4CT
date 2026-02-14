using System;
using System.Diagnostics;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides utility methods for gathering process-related information and formatting memory sizes.
    /// </summary>
    public static class ProcessUtils
    {
        /// <summary>
        /// Common size units for displaying byte values in a human-readable format.
        /// </summary>
        private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        /// <summary>
        /// Obtains the total CPU time (in milliseconds) and memory usage (in bytes) for the current process.
        /// </summary>
        /// <param name="elapsedTime">
        /// When this method returns, contains the total CPU time, in milliseconds,
        /// that the process has used. This value comes from <see cref="Process.TotalProcessorTime"/>.
        /// </param>
        /// <param name="usedMemory">
        /// When this method returns, contains the total allocated memory, in bytes,
        /// as reported by <see cref="GC.GetTotalMemory(bool)"/>.
        /// </param>
        public static void GetProcessData(out double elapsedTime, out long usedMemory)
        {
            var process = Process.GetCurrentProcess();
            process.Refresh();
            elapsedTime = process.TotalProcessorTime.TotalMilliseconds;
            usedMemory = GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Formats a byte value into a human-readable string using common size suffixes (e.g., KB, MB, GB).
        /// </summary>
        /// <param name="value">The size, in bytes, to be formatted.</param>
        /// <param name="decimalPlaces">
        /// The number of decimal places to include in the formatted output.
        /// The default value is <c>1</c>.
        /// </param>
        /// <returns>
        /// A string representation of <paramref name="value"/> that includes the appropriate size suffix.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="decimalPlaces"/> is less than zero.
        /// </exception>
        public static string SizeSuffix(long value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
            }

            // Handle negative values by flipping the sign and adding a '-' prefix.
            if (value < 0)
            {
                return "-" + SizeSuffix(-value, decimalPlaces);
            }

            // Return immediately if there are zero bytes.
            if (value == 0)
            {
                return string.Format("{0:n" + decimalPlaces + "} bytes", 0);
            }

            // Determine the appropriate size suffix based on log base 1024.
            var mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) is equivalent to 2^(10*mag).
            var adjustedSize = (decimal)value / (1L << (mag * 10));

            // If the value is large enough, adjust to the next unit.
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", adjustedSize, SizeSuffixes[mag]);
        }
    }
}
