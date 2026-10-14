using System;

namespace Mythosia.Integrity.CRC
{
    /// <summary>Calculates CRC64 incrementally without retaining the appended bytes.</summary>
    /// <remarks>
    /// The result is the same as calculating CRC64 over all appended bytes together.
    /// Separate instances can be used independently. An instance is not thread-safe.
    /// </remarks>
    public sealed class CRC64Accumulator
    {
        private ulong _state = CRC64Core.InitialState;

        /// <summary>Gets the selected CRC64 algorithm.</summary>
        public CRC64Type Type { get; }

        /// <summary>Creates an empty CRC64 accumulator for the selected algorithm.</summary>
        /// <param name="type">The CRC64 algorithm. Only NVME is supported.</param>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not supported.</exception>
        public CRC64Accumulator(CRC64Type type = CRC64Type.NVME)
        {
            CRC64Core.ValidateType(type);
            Type = type;
        }

        /// <summary>Appends bytes to the current calculation without retaining the source.</summary>
        /// <param name="source">The next bytes to consume immediately. An empty span leaves the state unchanged.</param>
        public void Append(ReadOnlySpan<byte> source) => _state = CRC64Core.Append(_state, source);

        /// <summary>Gets the current CRC64 value without changing or finalizing the calculation state.</summary>
        /// <returns>The CRC64 value for all appended bytes, or zero when empty. More bytes may be appended afterward.</returns>
        public ulong GetCurrentValue() => ~_state;

        /// <summary>Restores the empty initial state so the accumulator can be reused with the same algorithm.</summary>
        public void Reset() => _state = CRC64Core.InitialState;
    }
}
