using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.CRC
{
    /// <summary>Provides CRC64 calculation and frame encoding for byte sequences.</summary>
    public static class ExtensionForCRC64
    {
        /// <summary>Calculates the numeric CRC64 value for a byte sequence.</summary>
        /// <param name="data">The bytes to consume once.</param>
        /// <param name="type">The CRC64 algorithm: NVME, ECMA182 or XZ. The default is NVME.</param>
        /// <returns>The CRC64 value, or zero for an empty sequence.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not supported.</exception>
        public static ulong CRC64(this IEnumerable<byte> data, CRC64Type type = CRC64Type.NVME)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            CRC64Core.ValidateType(type);
            return CRC64Core.Compute(data, type);
        }

        /// <summary>Copies a byte sequence and appends its CRC64 digest in native byte order.</summary>
        /// <param name="data">The payload bytes to consume once.</param>
        /// <param name="type">The CRC64 algorithm: NVME, ECMA182 or XZ. The default is NVME.</param>
        /// <returns>The payload followed by its eight CRC64 digest bytes.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not supported.</exception>
        public static IEnumerable<byte> WithCRC64(this IEnumerable<byte> data, CRC64Type type = CRC64Type.NVME)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new CRC64(type).Encode(data);
        }
    }
}
