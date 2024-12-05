using System;
using System.Collections.Generic;
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


        /// <summary>
        /// Safely reads all bytes from a file asynchronously.
        /// Returns an empty array if the file does not exist.
        /// </summary>
        /// <param name="filePath">The file path to read from</param>
        /// <returns>Byte array containing the contents of the file, or empty array if the file does not exist</returns>
        public static async Task<byte[]> ReadAllBytesAsync(this string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return Array.Empty<byte>();
                }

                return await File.ReadAllBytesAsync(filePath);
            }
            catch (Exception ex) when (
                ex is FileNotFoundException ||
                ex is DirectoryNotFoundException ||
                ex is PathTooLongException ||
                ex is UnauthorizedAccessException)
            {
                // Add logging here if needed
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Safely reads all text from a file asynchronously.
        /// Returns an empty string if the file does not exist.
        /// </summary>
        /// <param name="filePath">The file path to read from</param>
        /// <param name="encoding">The encoding to use (default: UTF8)</param>
        /// <returns>String containing the contents of the file, or empty string if the file does not exist</returns>
        public static async Task<string> ReadAllTextAsync(this string filePath, Encoding? encoding = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return string.Empty;
                }

                return await File.ReadAllTextAsync(filePath, encoding ?? Encoding.UTF8);
            }
            catch (Exception ex) when (
                ex is FileNotFoundException ||
                ex is DirectoryNotFoundException ||
                ex is PathTooLongException ||
                ex is UnauthorizedAccessException)
            {
                // Add logging here if needed
                return string.Empty;
            }
        }


        /// <summary>
        /// Safely retrieves a list of files from a directory path.
        /// Returns an empty array if the directory does not exist.
        /// </summary>
        /// <param name="directoryPath">The directory path to search</param>
        /// <param name="searchPattern">The search pattern (default: "*")</param>
        /// <param name="searchOption">The search option (default: TopDirectoryOnly)</param>
        /// <returns>An array of file paths</returns>
        public static string[] GetAllFiles(this string directoryPath,
            string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                {
                    return Array.Empty<string>();
                }

                return Directory.GetFiles(directoryPath, searchPattern, searchOption);
            }
            catch (Exception ex) when (
                ex is UnauthorizedAccessException ||
                ex is PathTooLongException ||
                ex is DirectoryNotFoundException)
            {
                // Add logging here if needed
                return Array.Empty<string>();
            }
        }
    }
}
