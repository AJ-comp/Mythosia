using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.IO
{
    public static class StringExtension
    {

        /// <summary>
        /// Checks if the specified file path is valid and the file name does not contain any invalid characters.
        /// </summary>
        /// <param name="fullPath">The full file path to validate, including the file name and directory path.</param>
        /// <returns>
        ///   <c>true</c> if the file path and file name are valid; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method checks whether the provided file path is a fully qualified path, contains no invalid characters in the directory path, 
        /// and the file name does not contain invalid characters as defined by the operating system.
        /// </remarks>
        public static bool IsValidFilePathAndName(this string fullPath)
        {
            // 1. Check if the path is a fully qualified absolute path
            if (!Path.IsPathFullyQualified(fullPath) || !Path.IsPathRooted(fullPath))
                return false;

            // 2. Check if the path contains any invalid characters
            if (fullPath.Any(c => Path.GetInvalidPathChars().Contains(c)))
                return false;

            // 3. Check if the file name contains any invalid characters
            string fileName = Path.GetFileName(fullPath);
            if (fileName.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
                return false;

            return true;
        }


        /// <summary>
        /// Creates the directory if it does not exist, and asynchronously writes a collection of bytes (including byte arrays) to the specified full path.
        /// Supports byte arrays and other IEnumerable<byte> types.
        /// </summary>
        /// <param name="fullPath">The full path where the byte collection will be written.</param>
        /// <param name="data">The collection of bytes (supports byte[] and other IEnumerable<byte>) to write to the file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public static async Task WriteBytesAsync(this string fullPath, IEnumerable<byte> data, CancellationToken cancellationToken = default)
        {
            // If the directory does not exist, create it
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Asynchronously write the byte collection to the file with cancellation support
            await File.WriteAllBytesAsync(fullPath, data.AsOrToArray(), cancellationToken);
        }

        /// <summary>
        /// Creates the directory if it does not exist, and asynchronously writes text to the specified full path.
        /// </summary>
        /// <param name="fullPath">The full path where the text will be written.</param>
        /// <param name="content">The text content to write to the file.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public static async Task WriteTextAsync(this string fullPath, string content, CancellationToken cancellationToken = default)
        {
            // If the directory does not exist, create it
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Asynchronously write the text to the file with cancellation support
            await File.WriteAllTextAsync(fullPath, content, cancellationToken);
        }
    }
}
