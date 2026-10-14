using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Provides Adler-32 calculation and frame encoding for byte sequences.</summary>
    public static class ExtensionForAdler32
    {
        /// <summary>Calculates the numeric Adler-32 value for a byte sequence.</summary>
        /// <param name="data">The bytes to consume once.</param>
        /// <returns>The Adler-32 value, or one for an empty sequence.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static uint Adler32(this IEnumerable<byte> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return Adler32Core.Compute(data);
        }

        /// <summary>Copies a byte sequence and appends its Adler-32 checksum in native byte order.</summary>
        /// <remarks>The zlib format requires big-endian checksum bytes; serialize the numeric value explicitly for that format.</remarks>
        /// <param name="data">The payload bytes to consume once.</param>
        /// <returns>The payload followed by its four Adler-32 checksum bytes.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static IEnumerable<byte> WithAdler32(this IEnumerable<byte> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new Adler32().Encode(data);
        }
    }
}
