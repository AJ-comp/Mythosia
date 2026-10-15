using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.CRC
{
    /// <summary>Provides CRC32C (Castagnoli) calculation and frame encoding for byte sequences.</summary>
    public static class ExtensionForCRC32C
    {
        /// <summary>Calculates the numeric CRC32C value for a byte sequence.</summary>
        /// <remarks>Use <c>data.CRC32(CRC32Type.Castagnoli)</c> for the same result through the CRC32 algorithm selector.</remarks>
        /// <param name="data">The bytes to consume once.</param>
        /// <returns>The CRC32C value, or zero for an empty sequence.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        [Obsolete("CRC32C is obsolete and will be removed in a future version. Use CRC32(CRC32Type.Castagnoli) instead.")]
        public static uint CRC32C(this IEnumerable<byte> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return CRC32CCore.Compute(data);
        }

        /// <summary>Copies a byte sequence and appends its CRC32C digest in native byte order.</summary>
        /// <param name="data">The payload bytes to consume once.</param>
        /// <returns>The payload followed by its four CRC32C digest bytes.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static IEnumerable<byte> WithCRC32C(this IEnumerable<byte> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new CRC32C().Encode(data);
        }
    }
}
