using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Provides numeric XXH32 hashes and native-byte-order frames.</summary>
    public static class ExtensionForXxHash32
    {
        /// <summary>Calculates the numeric XXH32 hash for a sequence.</summary>
        /// <param name="data">The input consumed once.</param>
        /// <param name="seed">The XXH32 seed, zero by default.</param>
        /// <returns>The numeric hash.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static uint XxHash32(this IEnumerable<byte> data, uint seed = 0)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return XxHash32Core.Compute(data, seed);
        }

        /// <summary>Appends an XXH32 hash in native byte order.</summary>
        /// <remarks>The xxHash canonical digest is big-endian. This method follows the library's native framing convention.</remarks>
        /// <param name="data">The payload consumed once.</param>
        /// <param name="seed">The XXH32 seed, zero by default.</param>
        /// <returns>The payload and trailing hash bytes.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static IEnumerable<byte> WithXxHash32(this IEnumerable<byte> data, uint seed = 0)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new XxHash32(seed).Encode(data);
        }
    }
}
