using System;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates Adler-32 incrementally without retaining the appended bytes.</summary>
    /// <remarks>
    /// The result is the same as calculating Adler-32 over all appended bytes together.
    /// Separate instances can be used independently. An instance is not thread-safe.
    /// </remarks>
    public sealed class Adler32Accumulator
    {
        private uint _state = Adler32Core.InitialState;

        /// <summary>Creates an empty Adler-32 accumulator.</summary>
        public Adler32Accumulator()
        {
        }

        /// <summary>Appends bytes to the current calculation without retaining the source.</summary>
        /// <param name="source">The next bytes to consume immediately. An empty span leaves the state unchanged.</param>
        public void Append(ReadOnlySpan<byte> source) => _state = Adler32Core.Append(_state, source);

        /// <summary>Gets the current Adler-32 value without changing or finalizing the calculation state.</summary>
        /// <returns>The Adler-32 value for all appended bytes, or one when empty. More bytes may be appended afterward.</returns>
        public uint GetCurrentValue() => _state;

        /// <summary>Restores the empty initial state so the accumulator can be reused.</summary>
        public void Reset() => _state = Adler32Core.InitialState;
    }
}
