using System;
using System.Collections.Generic;
using System.IO;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Represents a series of file utility methods.
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// Tries to find a file with the specified <paramref name="originalPath"/> starting from the given <paramref name="basePath"/>.
        /// </summary>
        /// <param name="basePath">The base directory to begin the search.</param>
        /// <param name="originalPath">The relative or absolute path of the file to find.</param>
        /// <param name="recursively">
        /// <see langword="true"/> to search within all subdirectories of <paramref name="basePath"/>;
        /// <see langword="false"/> to search only in <paramref name="basePath"/>.
        /// </param>
        /// <returns>
        /// The fully qualified path to the file if it is found; otherwise, <see langword="null"/>.
        /// </returns>
        public static string FindFile(string basePath, string originalPath, bool recursively = false)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
            {
                return null;
            }
            originalPath = SanitizePath(originalPath);
            var filename = GetFilename(originalPath);
            if (!Directory.Exists(basePath))
            {
                return null;
            }
            var files = Directory.GetFiles(basePath, filename, recursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            return files.Length > 0 ? files[0] : null;
        }

        /// <summary>
        /// Gets the file name (including its extension) from the specified <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The full path from which to extract the short file name.</param>
        /// <returns>The short file name (with extension), or <see langword="null"/> if <paramref name="filename"/> is null or empty.</returns>
        public static string GetShortFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return null;
            }
            filename = SanitizePath(filename);
            var indexOfSlash = filename.LastIndexOf("/");
            if (indexOfSlash >= 0)
            {
                return filename.Substring(indexOfSlash + 1);
            }
            return filename;
        }

        /// <summary>
        /// Gets the directory portion of the specified <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The full path from which to extract the directory.</param>
        /// <returns>The directory portion of the path, or <see langword="null"/> if none is found or <paramref name="filename"/> is null or empty.</returns>
        public static string GetFileDirectory(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return null;
            }
            filename = SanitizePath(filename);
            var indexOfSlash = filename.LastIndexOf("/");
            if (indexOfSlash >= 0)
            {
                return filename.Substring(0, indexOfSlash);
            }
            return null;
        }

        /// <summary>
        /// Gets the file name (without the extension) from the specified <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The full path from which to extract the file name without extension.</param>
        /// <returns>
        /// The file name without extension, or <see langword="null"/> if <paramref name="filename"/> is null or empty.
        /// If the file has no extension, returns <paramref name="filename"/> itself.
        /// </returns>
        public static string GetFilenameWithoutExtension(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return null;
            }
            filename = SanitizePath(filename);
            var indexOfDot = filename.LastIndexOf('.');
            if (indexOfDot < 0)
            {
                return filename;
            }
            var indexOfSlash = filename.LastIndexOf("/", StringComparison.Ordinal);
            if (indexOfSlash >= 0)
            {
                return filename.Substring(indexOfSlash + 1, indexOfDot - indexOfSlash - 1);
            }
            return filename.Substring(0, indexOfDot);
        }

        /// <summary>
        /// Gets the file extension from the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The file path to process.</param>
        /// <param name="includeDot">
        /// <see langword="true"/> to include the dot (.) in the returned extension; 
        /// <see langword="false"/> to exclude it.
        /// </param>
        /// <returns>The file extension in lowercase, or <see langword="null"/> if none is found.</returns>
        public static string GetFileExtension(string path, bool includeDot = true)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            path = SanitizePath(path);
            var lastDot = path.LastIndexOf('.');
            if (lastDot < 0)
            {
                return null;
            }
            return path.Substring(includeDot ? lastDot : lastDot + 1).ToLowerInvariant();
        }

        /// <summary>
        /// Retrieves the file name from the given <paramref name="path"/>.
        /// Uses <see cref="Path.GetFileName(string)"/> for the underlying operation,
        /// but falls back to string manipulation if the result matches the entire <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The full file path.</param>
        /// <returns>The file name portion of the path, or <see langword="null"/> if <paramref name="path"/> is null or empty.</returns>
        public static string GetFilename(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            path = SanitizePath(path);
            var filename = Path.GetFileName(path);
            if (path == filename)
            {
                var indexOfSlash = path.LastIndexOf("/");
                if (indexOfSlash >= 0)
                {
                    return path.Substring(indexOfSlash + 1);
                }
                return path;
            }
            return filename;
        }

        /// <summary>
        /// Synchronously loads all file data from the specified <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The full path to the file to read.</param>
        /// <returns>
        /// A byte array containing the file data, or an empty array if <paramref name="filename"/> is null, empty, 
        /// or an exception occurs while reading the file.
        /// </returns>
        public static byte[] LoadFileData(string filename)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filename))
                {
                    return new byte[0];
                }
                return File.ReadAllBytes(SanitizePath(filename));
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }

        /// <summary>
        /// Creates and returns a <see cref="FileStream"/> for the specified <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The full path of the file to open.</param>
        /// <returns>
        /// A <see cref="FileStream"/> in <see cref="FileMode.Open"/> mode with read-only access,
        /// or <see langword="null"/> if <paramref name="filename"/> is null, empty, or an exception occurs.
        /// </returns>
        public static FileStream LoadFileStream(string filename)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filename))
                {
                    return null;
                }
                return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Normalizes a file path by removing invalid filename characters and replacing backslashes with forward slashes.
        /// </summary>
        /// <param name="path">The original path.</param>
        /// <returns>The sanitized path, or <see langword="null"/> if <paramref name="path"/> is null or empty.</returns>
        public static string SanitizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            var invalids = Path.GetInvalidFileNameChars();
            // Remove certain valid path-specific characters from the invalid list
            ArrayUtils.Remove(ref invalids, Path.AltDirectorySeparatorChar);
            ArrayUtils.Remove(ref invalids, Path.DirectorySeparatorChar);
            ArrayUtils.Remove(ref invalids, Path.PathSeparator);
            ArrayUtils.Remove(ref invalids, Path.VolumeSeparatorChar);
            var split = path.Split(invalids, StringSplitOptions.RemoveEmptyEntries);
            path = string.Join("_", split);
            path = path.Replace('\\', '/');
            return path;
        }

        /// <summary>
        /// Attempts to save an embedded file to persistent storage, optionally decoding its data.
        /// </summary>
        /// <param name="assetLoaderContext">An <see cref="AssetLoaderContext"/> containing context information for the asset loading process.</param>
        /// <param name="objectName">The name of the object associated with the embedded file.</param>
        /// <param name="filename">The name of the embedded file.</param>
        /// <param name="inputData">An <see cref="IEnumerator{Byte}"/> that yields the raw file data, byte by byte.</param>
        /// <param name="finalPath">
        /// When this method returns <see langword="true"/>, contains the path where the file was saved;
        /// otherwise, <see langword="null"/>.
        /// </param>
        /// <param name="decoder">
        /// An optional action that can decode the <paramref name="inputData"/> before writing it to disk.
        /// If <see langword="null"/>, the data is written directly to the file.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the file was successfully saved (or already exists in the target location);
        /// <see langword="false"/> otherwise.
        /// </returns>
        public static bool TrySaveFileAtPersistentDataPath(
            AssetLoaderContext assetLoaderContext,
            string objectName,
            string filename,
            IEnumerator<byte> inputData,
            out string finalPath,
            Action<IEnumerator<byte>, string> decoder = null
        )
        {
            finalPath = null;
            if (!assetLoaderContext.Options.ExtractEmbeddedData)
            {
                return false;
            }
            if (assetLoaderContext.Filename == null || filename == null)
            {
                return false;
            }

            var shortFilename = SanitizePath(GetShortFilename(filename));
            var localPath = GetFilenameWithoutExtension(assetLoaderContext.Filename);
            var basePath = assetLoaderContext.Options.EmbeddedDataExtractionPath ?? assetLoaderContext.PersistentDataPath;
            var folder = SanitizePath($"{basePath}/{localPath}({assetLoaderContext.ModificationDate}).tlc");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            finalPath = $"{folder}/{shortFilename}";
            if (File.Exists(finalPath))
            {
                return true;
            }

            if (decoder != null)
            {
                decoder.Invoke(inputData, finalPath);
            }
            else
            {
                using (var binaryWriter = new BinaryWriter(File.Create(finalPath)))
                {
                    while (inputData.MoveNext())
                    {
                        binaryWriter.Write(inputData.Current);
                    }
                }
            }
            return true;
        }
    }
}
