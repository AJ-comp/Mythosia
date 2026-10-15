using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Provides numeric XXH64 hashes and native-byte-order frames.</summary>
    public static class ExtensionForXxHash64
    {
        /// <summary>Calculates the numeric XXH64 hash for a sequence.</summary>
        /// <param name="data">The input consumed once.</param>
        /// <param name="seed">The XXH64 seed, zero by default.</param>
        /// <returns>The numeric hash.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static ulong XxHash64(this IEnumerable<byte> data, ulong seed = 0)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return XxHash64Core.Compute(data, seed);
        }

        /// <summary>Appends an XXH64 hash in native byte order.</summary>
        /// <remarks>The xxHash canonical digest is big-endian. This method follows the library's native framing convention.</remarks>
        /// <param name="data">The payload consumed once.</param>
        /// <param name="seed">The XXH64 seed, zero by default.</param>
        /// <returns>The payload and trailing hash bytes.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static IEnumerable<byte> WithXxHash64(this IEnumerable<byte> data, ulong seed = 0)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new XxHash64(seed).Encode(data);
        }
    }
}
