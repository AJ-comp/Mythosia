using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates OpenZFS Fletcher-4 over complete 32-bit input words.</summary>
    public static class ExtensionForFletcher4
    {
        /// <summary>Calculates the four wrapping 64-bit sums using the selected input word byte order.</summary>
        /// <exception cref="System.ArgumentNullException">The source is null.</exception>
        /// <exception cref="System.ArgumentException">The length is not divisible by four.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The byte order is unsupported.</exception>
        public static Fletcher4Value Fletcher4(this IEnumerable<byte> source,
            Fletcher4ByteOrder byteOrder = Fletcher4ByteOrder.LittleEndian) => global::Mythosia.Integrity.Checksum.Fletcher4.ComputeValue(source, byteOrder);

        /// <summary>Appends a 32-byte A/B/C/D digest with each 64-bit sum in native byte order.</summary>
        /// <remarks>The payload length must be divisible by four. This is the library's generic frame format.</remarks>
        public static IEnumerable<byte> WithFletcher4(this IEnumerable<byte> source,
            Fletcher4ByteOrder byteOrder = Fletcher4ByteOrder.LittleEndian) => new Fletcher4(byteOrder).Encode(source);
    }
}
