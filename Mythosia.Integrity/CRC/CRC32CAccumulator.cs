using System;

namespace Mythosia.Integrity.CRC
{
    /// <summary>Calculates CRC32C (Castagnoli) incrementally without retaining the appended bytes.</summary>
    /// <remarks>
    /// The result is the same as calculating CRC32C over all appended bytes together.
    /// Separate instances can be used independently. An instance is not thread-safe.
    /// </remarks>
    public sealed class CRC32CAccumulator
    {
        private uint _state = CRC32CCore.InitialState;

        /// <summary>Creates an empty CRC32C accumulator.</summary>
        public CRC32CAccumulator()
        {
        }

        /// <summary>Appends bytes to the current calculation without retaining the source.</summary>
        /// <param name="source">The next bytes to consume immediately. An empty span leaves the state unchanged.</param>
        public void Append(ReadOnlySpan<byte> source) => _state = CRC32CCore.Append(_state, source);

        /// <summary>Gets the current CRC32C value without changing or finalizing the calculation state.</summary>
        /// <returns>The CRC32C value for all appended bytes, or zero when empty. More bytes may be appended afterward.</returns>
        public uint GetCurrentValue() => ~_state;

        /// <summary>Restores the empty initial state so the accumulator can be reused.</summary>
        public void Reset() => _state = CRC32CCore.InitialState;
    }
}
