using System;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates the Internet checksum incrementally without retaining the appended bytes.</summary>
    /// <remarks>
    /// Combines bytes into big-endian 16-bit words and calculates their one's-complement sum as defined in RFC 1071.
    /// An odd trailing byte remains pending across appends and is padded with a zero low byte only for the current result.
    /// Separate instances can be used independently. An instance is not thread-safe.
    /// </remarks>
    public sealed class InternetChecksumAccumulator
    {
        private InternetChecksumCore _state;

        /// <summary>Creates an empty Internet checksum accumulator.</summary>
        public InternetChecksumAccumulator()
        {
        }

        /// <summary>Appends bytes to the current calculation without retaining the source.</summary>
        /// <param name="source">The next bytes to consume immediately. An empty span leaves the state unchanged.</param>
        public void Append(ReadOnlySpan<byte> source) => _state.Append(source);

        /// <summary>Gets the current Internet checksum without changing or finalizing the calculation state.</summary>
        /// <returns>The checksum for all appended bytes, or 0xFFFF when empty. More bytes may be appended afterward.</returns>
        public ushort GetCurrentValue() => _state.GetCurrentValue();

        /// <summary>Restores the empty initial state so the accumulator can be reused.</summary>
        public void Reset() => _state = default;
    }
}
