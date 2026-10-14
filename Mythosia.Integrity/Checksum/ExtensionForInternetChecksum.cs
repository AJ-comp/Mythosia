using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Provides Internet checksum calculation and frame encoding for byte sequences.</summary>
    public static class ExtensionForInternetChecksum
    {
        /// <summary>Calculates the Internet checksum of big-endian 16-bit words as defined in RFC 1071.</summary>
        /// <remarks>
        /// Pads an odd trailing byte with a zero low byte for calculation only. Supply any required protocol
        /// pseudo-header and cleared checksum field explicitly; a zero result is returned unchanged.
        /// Serialize the numeric result in big-endian order when writing a network protocol checksum field.
        /// </remarks>
        /// <param name="data">The bytes to consume once.</param>
        /// <returns>The numeric Internet checksum, or 0xFFFF for an empty sequence.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static ushort InternetChecksum(this IEnumerable<byte> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return InternetChecksumCore.Compute(data);
        }

        /// <summary>Copies a byte sequence and appends its Internet checksum in native byte order.</summary>
        /// <remarks>This library frame uses native byte order; network protocols require explicit big-endian serialization.</remarks>
        /// <param name="data">The payload bytes to consume once.</param>
        /// <returns>The payload followed by its two Internet checksum bytes.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static IEnumerable<byte> WithInternetChecksum(this IEnumerable<byte> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new InternetChecksum().Encode(data);
        }
    }
}
