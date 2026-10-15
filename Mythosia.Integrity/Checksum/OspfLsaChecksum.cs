using System;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates and validates the OSPFv2/OSPFv3 LSA Fletcher checksum defined by RFC 2328 section 12.1.7.</summary>
    /// <remarks>
    /// Supply exactly one complete LSA, including its 20-byte LSA header, rather than an OSPF packet or a header for a longer LSA.
    /// The two-byte LS age is excluded. The checksum occupies bytes 16 and 17 in network byte order.
    /// The LSA length field at bytes 18 and 19 must equal the supplied length. LSA body semantics are not parsed.
    /// </remarks>
    public static class OspfLsaChecksum
    {
        private const int HeaderLength = 20;
        private const int ChecksumOffset = 16;

        /// <summary>Calculates the checksum to place at bytes 16 and 17, ignoring their current contents.</summary>
        /// <exception cref="ArgumentException">The complete LSA length or its length field is invalid.</exception>
        public static ushort Compute(ReadOnlySpan<byte> lsa)
        {
            ValidateLength(lsa);
            int first = 0, second = 0;
            for (int index = 2; index < lsa.Length; index++)
            {
                if (index != ChecksumOffset && index != ChecksumOffset + 1) first = (first + lsa[index]) % 255;
                second = (second + first) % 255;
            }
            int high = ((lsa.Length - ChecksumOffset - 1) * first - second) % 255;
            if (high <= 0) high += 255;
            int low = 510 - first - high;
            if (low > 255) low -= 255;
            return (ushort)((high << 8) | low);
        }

        /// <summary>Replaces only the LSA checksum field with the calculated network-order bytes.</summary>
        /// <exception cref="ArgumentException">The complete LSA length or its length field is invalid.</exception>
        public static void Write(Span<byte> lsa)
        {
            ushort checksum = Compute(lsa);
            lsa[ChecksumOffset] = (byte)(checksum >> 8);
            lsa[ChecksumOffset + 1] = (byte)checksum;
        }

        /// <summary>Checks the complete LSA length and both modulo-255 residues, excluding LS age.</summary>
        /// <returns>False for a malformed length, the reserved zero checksum or incorrect residues.</returns>
        public static bool IsValid(ReadOnlySpan<byte> lsa)
        {
            if (!HasValidLength(lsa) || (lsa[ChecksumOffset] == 0 && lsa[ChecksumOffset + 1] == 0)) return false;
            int first = 0, second = 0;
            for (int index = 2; index < lsa.Length; index++)
            {
                first = (first + lsa[index]) % 255;
                second = (second + first) % 255;
            }
            return first == 0 && second == 0;
        }

        private static bool HasValidLength(ReadOnlySpan<byte> lsa) => lsa.Length >= HeaderLength
            && lsa.Length <= ushort.MaxValue && ((lsa[18] << 8) | lsa[19]) == lsa.Length;

        private static void ValidateLength(ReadOnlySpan<byte> lsa)
        {
            if (!HasValidLength(lsa))
                throw new ArgumentException("Supply one complete LSA of at least 20 bytes matching its 16-bit length field.", nameof(lsa));
        }
    }
}
